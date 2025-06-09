using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[System.Serializable]
public class CardData //Datatype for storing CardData parsed from the JSON
{
    public int id;
    public string name;
    public string card_type;
    public int? level;
    public string color;
    public string form;
    public string attribute;
    public int? dp;
    public int? play_cost;
    public List<DigivolveCost> digivolve_costs;
    public string image_path;
    public List<EffectData> effects;
    public List<EffectData> inheritedEffects;

    //Debug function to be able to log card data
    public override string ToString()
    {
        return $"ID: {id}, Name: {name}, Type: {card_type}, Level: {level}, PlayCost: {play_cost}, Color: {color}, Form: {form}, DP: {dp}";
    }

    [System.Serializable]
    public class DigivolveCost //Datatype for representing Digivolution conditions
    {
        public string color;
        public int cost;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handZone;
    [SerializeField] private Transform opponentHandZone;
    [SerializeField] private Transform opponentBattleZone;
    [SerializeField] private Transform opponentTamerZone;
    [SerializeField] private Transform breedingZone;
    [SerializeField] private MemoryGaugeManager memoryManager;
    [SerializeField] private Sprite cardBackSprite;
    [SerializeField] private GameObject securityRevealPrefab;
    [SerializeField] private Transform canvasTransform;

    private List<CardData> deckguide;
    private Dictionary<int, CardData> idToData = new Dictionary<int, CardData>();
    private Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();

    private List<int> digieggs = new List<int>();
    private List<int> deck = new List<int>();
    private List<int> player2Deck = new List<int>();
    private List<int> player2Hand = new List<int>();
    private List<int> player2Eggs = new List<int>();
    private List<int> player1SecurityStack = new List<int>();
    private List<int> player2SecurityStack = new List<int>();
    private List<int> player1Trash = new List<int>();
    private List<int> player2Trash = new List<int>();
    private int currentMemory = 0;
    private int activePlayer = 0;
    private bool isGameOver = false;

    public bool isHatchingSlotOccupied = false;
    public int localPlayerId = 0;

    private void Awake()
    {
        Instance = this;
        currentMemory = memoryManager.GetCurrentMemory();
    }

    private void Start()
    {
        LoadDeckGuide();

        InitializeDeck();
        DrawStartingHand(5);

        InitializeOpponentDeck();

        InitializeSecurityStacks();

        StartTurn(0);
    }

    private void GameOver()
    {
        Time.timeScale = 0;
        Debug.Log("Game Over — Freezing time.");
    }

    private void LoadDeckGuide()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/Agumon-Deck/AgumonDeckJSON");
        if (jsonFile == null)
        {
            Debug.LogError("Deck JSON file not found.");
            return;
        }

        JArray jsonArray = JArray.Parse(jsonFile.text);
        deckguide = new List<CardData>();

        foreach (var token in jsonArray)
        {
            CardData card = token.ToObject<CardData>();
            deckguide.Add(card);
            idToData[card.id] = card;

            if (card.card_type == "Digi-Egg")
                digieggs.Add(card.id);
            else
                deck.Add(card.id);

            // Load and cache image
            string path = card.image_path.Replace("./", "Cards/Agumon-Deck/").Replace(".jpg", "").Replace(".png", "");
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                idToSprite[card.id] = sprite;
            }
            else
            {
                Debug.LogWarning("Image not found for: " + path);
            }

            // Parse main effects
            if (token["effect"] is JArray effectArray)
            {
                card.effects = new List<EffectData>();
                foreach (var entry in effectArray)
                {
                    string type = entry["type"]?.ToString();
                    string trigger = entry["trigger"]?.ToString();
                    string keyword = entry["keyword"]?.ToString();

                    var innerEffect = entry["effect"];
                    int value = innerEffect?["value"]?.Value<int>() ?? 0;

                    card.effects.Add(new EffectData(
                        ParseTrigger(trigger),
                        ParseEffectType(type, keyword),
                        value
                    ));
                }
            }

            // Parse inherited effects
            if (token["inherited_effect"] is JObject inh)
            {
                string phase = inh["phase"]?.ToString();
                string innerType = inh["effect"]?["type"]?.ToString();
                int value = inh["effect"]?["value"]?.Value<int>() ?? 0;

                card.inheritedEffects = new List<EffectData>
                {
                    new EffectData(ParseTrigger(phase), ParseEffectType(innerType), value)
                };
            }
        }
    }

    private void InitializeDeck()
    {
        deck = deck.OrderBy(x => Random.value).ToList();
    }

    private void InitializeOpponentDeck()
    {
        player2Deck = new List<int>(deck);
        player2Eggs = new List<int>(digieggs);

        player2Deck = player2Deck.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < 5 && player2Deck.Count > 0; i++)
        {
            int cardId = player2Deck[0];
            player2Deck.RemoveAt(0);
            player2Hand.Add(cardId);
            SpawnCardToHand(cardId, opponentHandZone, 1);
        }
    }

    private void DrawStartingHand(int count)
    {
        for (int i = 0; i < count && deck.Count > 0; i++)
        {
            int cardId = deck[0];
            deck.RemoveAt(0);
            SpawnCardToHand(cardId, handZone, 0);
        }
    }

    private void SpawnCardToHand(int cardId, Transform zone, int ownerId)
    {
        GameObject cardGO = Instantiate(cardPrefab, zone);
        Image image = cardGO.GetComponent<Image>();

        Card card = cardGO.GetComponent<Card>();
        card.cardId = cardId;
        card.currentZone = Card.Zone.Hand;
        card.ownerId = ownerId;

        if (idToData.TryGetValue(cardId, out CardData data))
        {
            card.cardName = data.name;
            card.cardType = data.card_type;
            card.level = data.level;
            card.color = data.color;
            card.form = data.form;
            card.attribute = data.attribute;
            card.dp = data.dp;
            card.playCost = data.play_cost ?? 0;
            card.effects = data.effects ?? new List<EffectData>();
            card.inheritedEffects = data.inheritedEffects ?? new List<EffectData>();

            if (data.digivolve_costs != null && data.digivolve_costs.Count > 0)
            {
                card.digivolveCost = data.digivolve_costs
                    .Select(dc => new DigivolveCostEntry { color = dc.color, cost = dc.cost })
                    .ToList();
            }
            else
            {
                card.digivolveCost = null;
            }

            card.InitializeFlagsFromEffects();
        }

        if (idToSprite.TryGetValue(cardId, out Sprite sprite))
        {
            card.sprite = sprite;
        }

        if (ownerId == localPlayerId)
        {
            image.sprite = card.sprite;
        }
        else
        {
            image.sprite = cardBackSprite;
        }

    }

    private void InitializeSecurityStacks()
    {
        for (int i = 0; i < 5 && deck.Count > 0; i++)
        {
            int cardId = deck[0];
            deck.RemoveAt(0);
            player1SecurityStack.Add(cardId);
        }

        for (int i = 0; i < 5 && player2Deck.Count > 0; i++)
        {
            int cardId = player2Deck[0];
            player2Deck.RemoveAt(0);
            player2SecurityStack.Add(cardId);
        }
    }

    private void HatchDigiegg(int cardId)
    {
        GameObject cardGO = Instantiate(cardPrefab, breedingZone);
        Image image = cardGO.GetComponent<Image>();

        if (idToSprite.TryGetValue(cardId, out Sprite sprite))
        {
            image.sprite = sprite;
        }

        Card card = cardGO.GetComponent<Card>();
        card.cardId = cardId;
        card.currentZone = Card.Zone.BreedingActiveSlot;
        card.ownerId = 0;

        Destroy(card.GetComponent<CardDropHandler>());

        if (idToData.TryGetValue(cardId, out CardData data))
        {
            card.cardName = data.name;
            card.cardType = data.card_type;
            card.level = data.level;
            card.color = data.color;
            card.form = data.form;
            card.attribute = data.attribute;
            card.dp = data.dp;
            card.playCost = data.play_cost ?? 0;
            card.digivolveCost = data.digivolve_costs?.Select(dc => new DigivolveCostEntry { color = dc.color, cost = dc.cost }).ToList() ?? new List<DigivolveCostEntry>();
            card.effects = data.effects ?? new List<EffectData>();
            card.inheritedEffects = data.inheritedEffects ?? new List<EffectData>();

            card.InitializeFlagsFromEffects();
        }
    }

    public void DrawCardFromDeck(int playerId)
    {

        if (playerId == 0)
        {
            if (deck.Count > 0)
            {
                int cardId = deck[0];
                deck.RemoveAt(0);
                SpawnCardToHand(cardId, handZone, 0);
            }
            else
            {
                Debug.Log("Deck is empty. AI Wins");
                isGameOver = true;
                GameOver();
            }
        }
        else
        {
            if (player2Deck.Count > 0)
            {
                int cardId = player2Deck[0];
                player2Deck.RemoveAt(0);
                player2Hand.Add(cardId);
                SpawnCardToHand(cardId, opponentHandZone, 1);
            }
            else
            {
                Debug.Log("Player 2 Deck is empty. Player Wins");
                isGameOver = true;
                GameOver();
            }
        }

    }

    public bool DrawCardFromEggs()
    {
        if (!isHatchingSlotOccupied)
        {
            if (digieggs.Count > 0)
            {
                int cardId = digieggs[0];
                digieggs.RemoveAt(0);
                HatchDigiegg(cardId);
                isHatchingSlotOccupied = true;
            }
            else
            {
                Debug.Log("No more eggs to hatch");
                return false;
            }
        }
        else
        {
            Debug.Log("Breeding Active area is not available");
        }

        return true;
    }

    public void PlayCardToBattleArea(Card card)
    {
        if (!idToData.ContainsKey(card.cardId))
        {
            Debug.LogError("Card ID Missing in database");
        }

        CardData data = idToData[card.cardId];

        int cost = 0;

        if (data.play_cost.HasValue)
        {
            cost = data.play_cost.Value;
        }

        if (activePlayer == 0)
        {
            currentMemory -= cost;
        }
        else
        {
            currentMemory += cost;
        }

        currentMemory = Mathf.Clamp(currentMemory, -10, 10);

        memoryManager.SetMemory(currentMemory);

        CheckTurnSwitch();
    }

    public int RevealTopSecurityCard(int playerId)
    {
        if (playerId == 0 && player1SecurityStack.Count > 0)
        {
            int topCard = player1SecurityStack[0];
            player1SecurityStack.RemoveAt(0);
            return topCard;
        }
        else if (playerId == 1 && player2SecurityStack.Count > 0)
        {
            int topCard = player2SecurityStack[0];
            player2SecurityStack.RemoveAt(0);
            return topCard;
        }

        return -1; // Invalid or empty
    }

    public void StartTurn(int playerId)
    {
        if (isGameOver) return;

        activePlayer = playerId;

        DrawCardFromDeck(playerId);

        if (isGameOver) return;

        Debug.Log($"Player {playerId + 1}'s turn started.");

        if (playerId == 1)
        {
            RunAiTurn();
        }
    }

    public void EndTurn()
    {
        var allCards = FindObjectsOfType<Card>();

        foreach (var card in allCards)
        {
            if (card.currentZone == Card.Zone.BattleArea && card.ownerId == activePlayer)
            {
                card.canAttack = true;
            }
        }


        int nextPlayer = 0;

        if (activePlayer == 0)
        {
            nextPlayer = 1;
        }

        foreach (var card in FindObjectsOfType<Card>())
        {
            card.HideActionPanel();
        }

        StartTurn(nextPlayer);
    }

    public void ForceEndTurn()
    {
        if (activePlayer == 0)
        {
            currentMemory = 0;

            memoryManager.SetMemory(currentMemory);

            EndTurn();
        }
    }

    public void CheckTurnSwitch()
    {
        if ((activePlayer == 0 && currentMemory < 0) || (activePlayer == 1 && currentMemory > 0))
        {
            EndTurn();
        }
    }

    public int GetActivePlayer()
    {
        return activePlayer;
    }

    public void RunAiTurn()
    {
        Debug.Log("AI Turn Started");

        while (player2Hand.Count > 0)
        {
            if (activePlayer != 1)
            {
                return;
            }

            int cardId = player2Hand[0];

            Card card = opponentHandZone
            .GetComponentsInChildren<Card>()
            .FirstOrDefault(c => c.cardId == cardId);

            if (card == null)
            {
                Debug.LogWarning("Ai Card not found in their hand");
                return;
            }

            if (card.cardType == "Tamer")
            {
                card.transform.SetParent(opponentTamerZone);
                card.currentZone = Card.Zone.TamerArea;
            }
            else
            {
                card.transform.SetParent(opponentBattleZone);
                card.currentZone = Card.Zone.BattleArea;
            }

            card.GetComponent<Image>().sprite = card.sprite;

            player2Hand.RemoveAt(0);
            PlayCardToBattleArea(card);

            if (activePlayer != 1)
            {
                return;
            }
        }

        ForceEndTurn();
    }

    public void ResolveSecurityAttack(Card attacker)
    {
        int opponentId = 0;

        if (attacker.ownerId == 0)
        {
            opponentId = 1;
        }

        Card blocker = FindObjectsOfType<Card>().FirstOrDefault(card => card.ownerId == opponentId && card.isBlocking && card.currentZone = Card.Zone.BattleArea);

        if (blocker != null)
        {
            Debug.Log($"Blocker {blocker.cardName} intercepts the attack!");
            blocker.isBlocking = false;

            //TODO: Suspend indication
            blocker.canAttack = false;

            int attackerDP = attacker.dp ?? 0;
            int blockerDP = blocker.dp ?? 0;

            Debug.Log($"Battle: Attacker DP {attackerDP} vs Blocker DP {blockerDP}");

            if (attackerDP >= blockerDP)
            {
                Debug.Log($"{blocker.cardName} is deleted!");
                if (blocker.ownerId == 0)
                {
                    player1Trash.Add(blocker.cardId);
                }
                else
                {
                    player2Trash.Add(blocker.cardId);
                }
                Destroy(blocker.gameObject);
            }

            if (blockerDP >= attackerDP)
            {
                Debug.Log($"{attacker.cardName} is deleted!");
                if (attacker.ownerId == 0)
                {
                    player1Trash.Add(attacker.cardId);
                }
                else
                {
                    player2Trash.Add(attacker.cardId);
                }
                Destroy(attacker.gameObject);
            }

            return;
        }

        int securitycardId = RevealTopSecurityCard(opponentId);

        if (securitycardId == -1)
        {
            Debug.Log("Opponent has no security left, you win!");
            GameOver();
            return;
        }

        Debug.Log($"Revealed security card ID: {securitycardId}");

        if (idToSprite.TryGetValue(securitycardId, out Sprite sprite))
        {
            GameObject reveal = Instantiate(securityRevealPrefab, canvasTransform);
            Image img = reveal.GetComponent<Image>();
            img.sprite = sprite;

            RectTransform rect = reveal.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 440);
            rect.anchoredPosition = Vector2.zero;

            Destroy(reveal, 2f);
        }
        else
        {
            Debug.LogWarning("Sprite not found for Security card");
            return;
        }

        if (!idToData.TryGetValue(securitycardId, out CardData securityCardData))
        {
            Debug.LogWarning("Unable to fetch card data for revealed security card");
            return;
        }

        int attackerDP = attacker.dp ?? 0;
        int securityDP = securityCardData.dp ?? 0;

        Debug.Log($"Attacker DP: {attackerDP} vs Security DP: {securityDP}");

        if (securityDP >= attackerDP)
        {
            Debug.Log("Attacker is deleted.");

            if (attacker.ownerId == 0)
            {
                player1Trash.Add(attacker.cardId);
            }
            else
            {
                player2Trash.Add(attacker.cardId);
            }

            Destroy(attacker.gameObject);
        }
        else
        {
            Debug.Log("Attacker won, not deleted from play.");

            if (opponentId == 0)
            {
                player1Trash.Add(securitycardId);
            }
            else
            {
                player2Trash.Add(securitycardId);
            }
        }
    }

    public bool TryDigivolve(Card baseCard, Card newCard)
    {
        if (!newCard.CanDigivolveFrom(baseCard))
        {
            Debug.Log("Cannot Digivolve from card");
            return false;
        }

        int cost = newCard.GetDigivolveCost(baseCard);

        if (cost < 0)
        {
            Debug.Log("No Valid Digivolution cost entry found");
            return false;
        }

        if (activePlayer == 0)
        {
            currentMemory -= cost;
        }
        else
        {
            currentMemory += cost;
        }

        memoryManager.SetMemory(currentMemory);

        newCard.transform.SetParent(baseCard.transform);
        newCard.transform.localPosition = new Vector3(0, 30f, 0);
        baseCard.GetComponent<CanvasGroup>().blocksRaycasts = true;
        newCard.GetComponent<CanvasGroup>().blocksRaycasts = true;

        newCard.inheritedStack.Add(baseCard);
        newCard.currentZone = Card.Zone.BattleArea;

        Destroy(newCard.GetComponent<CardDropHandler>());

        if (baseCard.currentZone == Card.Zone.BreedingActiveSlot)
        {
            newCard.GetComponent<CanvasGroup>().blocksRaycasts = false;

            if (baseCard.GetComponent<CardDropHandler>() == null)
            {
                baseCard.gameObject.AddComponent<CardDropHandler>();
            }
            baseCard.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        CheckTurnSwitch();

        Debug.Log($"Successfully digivolved {baseCard.cardName} into {newCard.cardName}");

        return true;
    }

    public void PromoteDigivolvedCardToBattle(Card stackBase)
    {
        if (stackBase.transform.childCount == 0)
        {
            Debug.LogWarning("Promote failed: not a digivolution stack");
            return;
        }

        Card topCard = stackBase.GetComponentsInChildren<Card>().Last();

        CanvasGroup topGroup = topCard.GetComponent<CanvasGroup>();
        if (topGroup != null)
        {
            topGroup.blocksRaycasts = true;
        }

        topCard.canAttack = false;

        var baseDrag = stackBase.GetComponent<CardDropHandler>();
        if (baseDrag != null) Destroy(baseDrag);

        var baseGroup = stackBase.GetComponent<CanvasGroup>();
        if (baseGroup != null) baseGroup.blocksRaycasts = true;

        topCard.currentZone = Card.Zone.BattleArea;

        Debug.Log("Promoted top card in digivolution stack to active.");

        PlayCardToBattleArea(topCard);
    }

    private EffectTrigger ParseTrigger(string trigger)
    {
        return trigger switch
        {
            "when_attacking" => EffectTrigger.WhenAttacking,
            "when_digivolving" => EffectTrigger.WhenDigivolving,
            "your_turn" => EffectTrigger.YourTurn,
            "main" => EffectTrigger.MainPhase,
            _ => EffectTrigger.None
        };
    }

    private EffectType ParseEffectType(string type, string keyword = null)
    {
        if (type == "keyword" && keyword == "Blocker")
            return EffectType.Blocker;

        if (type == "keyword" && keyword == "Security Attack +1")
            return EffectType.ExtraSecurityAttack;

        return type switch
        {
            "modify_dp" => EffectType.ModifyDP,
            "gain_memory" => EffectType.GainMemory,
            "lose_memory" => EffectType.LoseMemory,
            "extra_security_attack" => EffectType.ExtraSecurityAttack,
            _ => EffectType.None
        };
    }

    public void ModifyMemory(int delta)
    {
        currentMemory += delta;
        currentMemory = Mathf.Clamp(currentMemory, -10, 10);
        memoryManager.SetMemory(currentMemory);
        CheckTurnSwitch();
    }

}
