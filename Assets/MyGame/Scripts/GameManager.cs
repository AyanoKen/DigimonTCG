using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Netcode; 

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

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handZone;
    [SerializeField] private Transform opponentHandZone;
    [SerializeField] private Transform opponentBattleZone;
    [SerializeField] public Transform opponentTamerZone;
    [SerializeField] public Transform playerTamerZone;
    [SerializeField] private Transform playerSecurityStackVisual;
    [SerializeField] private Transform opponentSecurityStackVisual;
    [SerializeField] private Transform breedingZone;
    [SerializeField] private MemoryGaugeManager memoryManager;
    [SerializeField] public Sprite cardBackSprite;
    [SerializeField] private GameObject securityRevealPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private GameObject endTurnBanner;
    [SerializeField] private SecurityBattlePreview battlePreviewPanel;


    [Header("Bottom Zones (Local Player Layout)")]
    [SerializeField] public Transform handZone_Bottom;
    [SerializeField] public Transform battleZone_Bottom;
    [SerializeField] public Transform tamerZone_Bottom;
    [SerializeField] public Transform breedingZone_Bottom;

    [Header("Top Zones (Opponent Layout)")]
    [SerializeField] public Transform handZone_Top;
    [SerializeField] public Transform battleZone_Top;
    [SerializeField] public Transform tamerZone_Top;
    [SerializeField] public Transform breedingZone_Top;

    private List<CardData> deckguide;
    private Dictionary<int, CardData> idToData = new Dictionary<int, CardData>();
    private Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();

    private List<int> player1Eggs = new List<int>();
    private List<int> player1Deck = new List<int>();
    private List<int> player2Deck = new List<int>();
    private List<int> player2Hand = new List<int>();
    private List<int> player2Eggs = new List<int>();
    private List<int> player1SecurityStack = new List<int>();
    private List<int> player2SecurityStack = new List<int>();
    private List<int> player1Trash = new List<int>();
    private List<int> player2Trash = new List<int>();
    private int currentMemory = 0;
    private bool isGameOver = false;

    public bool isHatchingSlotOccupied = false;
    public bool opponentHatchingOccupied = false;
    public int localPlayerId = 0;
    public int player1SecurityBuff = 0;
    public int player2SecurityBuff = 0;
    public bool turnTransition = false;

    public NetworkVariable<int> activePlayer = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        Instance = this;
        currentMemory = memoryManager.GetCurrentMemory();
    }

    private void Start()
    {
        LoadDeckGuide();

        if (IsServer)
        {
            InitializeDeck();
            DrawStartingHands(5);
            InitializeSecurityStacks();
            StartTurn();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        localPlayerId = (int)NetworkManager.Singleton.LocalClientId;

        activePlayer.OnValueChanged += OnTurnChanged;
    }

    private void OnTurnChanged(int previous, int current)
    {
        if ((ulong)current == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("🚀 It's my turn!");
            StartTurn();
        }
        else
        {
            Debug.Log("⏳ It's opponent's turn.");
        }
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
            {
                player1Eggs.Add(card.id);
                player2Eggs.Add(card.id);
            }
            else
            {
                player1Deck.Add(card.id);
                player2Deck.Add(card.id);
            }

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
                        string outerType = entry["type"]?.ToString();
                        string trigger = entry["trigger"]?.ToString();
                        string phase = entry["phase"]?.ToString();
                        string keyword = entry["keyword"]?.ToString();

                        // 🔧 Here is the critical fix you need to add:
                        if (outerType == "passive" && string.IsNullOrEmpty(trigger))
                        {
                            trigger = phase;
                        }

                        string innerType = entry["effect"]?["type"]?.ToString();
                        int value = entry["effect"]?["value"]?.Value<int>() ?? 0;
                        int conditionValue = entry["effect"]?["conditionValue"]?.Value<int>() ?? 0;

                        card.effects.Add(new EffectData(
                            ParseTrigger(trigger),
                            ParseEffectType(innerType ?? outerType, keyword),
                            value,
                            conditionValue
                        ));
                    }
                }

            // Parse inherited effects
            if (token["inherited_effect"] is JObject inh)
            {
                string phase = inh["phase"]?.ToString();
                string innerType = inh["effect"]?["type"]?.ToString();
                int value = inh["effect"]?["value"]?.Value<int>() ?? 0;
                int conditionValue = inh["effect"]?["conditionValue"]?.Value<int>() ?? 0;

                card.inheritedEffects = new List<EffectData>
                {
                    new EffectData(
                        ParseTrigger(phase),
                        ParseEffectType(innerType),
                        value,
                        conditionValue)
                };
            }
        }
    }

    private void InitializeDeck()
    {
        player1Deck = player1Deck.OrderBy(x => Random.value).ToList();
        player2Deck = player2Deck.OrderBy(x => Random.value).ToList();
    }

    private void DrawStartingHands(int count)
    {
        for (int i = 0; i < 5; i++)
        {
            DrawCardFromDeck(0); // Player 1
            DrawCardFromDeck(1); // Player 2
        }
    }

    public void DrawCardFromDeck(int playerId)
    {
        var deck = playerId == 0 ? player1Deck : player2Deck;

        if (deck.Count == 0)
        {
            Debug.Log($"Player {playerId + 1}'s deck is empty! They lose.");
            return;
        }

        int cardId = deck[0];
        deck.RemoveAt(0);

        SpawnCardToHand(cardId, (ulong)playerId);
    }

    private void SpawnCardToHand(int cardId, ulong clientId)
    {
        GameObject cardGO = Instantiate(cardPrefab);
        var netObj = cardGO.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.SpawnWithOwnership(clientId);
        }
        Image image = cardGO.GetComponent<Image>();

        Card card = cardGO.GetComponent<Card>();
        card.cardId = cardId;
        card.ownerId = (int)clientId;

        if (idToSprite.TryGetValue(cardId, out Sprite sprite))
        {
            card.sprite = sprite;
        }

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

        card.NotifyZoneChange(Card.Zone.Hand);
    }

    private void InitializeSecurityStacks()
    {
        for (int i = 0; i < 5 && player1Deck.Count > 0; i++)
        {
            int cardId = player1Deck[0];
            player1Deck.RemoveAt(0);
            player1SecurityStack.Add(cardId);
        }

        for (int i = 0; i < 5 && player2Deck.Count > 0; i++)
        {
            int cardId = player2Deck[0];
            player2Deck.RemoveAt(0);
            player2SecurityStack.Add(cardId);
        }
    }

    private void HatchDigiegg(int cardId, ulong clientId)
    {
        GameObject cardGO = Instantiate(cardPrefab);
        var netObj = cardGO.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.SpawnWithOwnership(clientId);
        }
        Image image = cardGO.GetComponent<Image>();

        Card card = cardGO.GetComponent<Card>();
        card.cardId = cardId;
        card.ownerId = (int)clientId;

        if (idToSprite.TryGetValue(cardId, out Sprite sprite))
        {
            card.sprite = sprite;
        }

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

        card.NotifyZoneChange(Card.Zone.BreedingActiveSlot);
    }

    public bool DrawCardFromEggs(int playerId)
    {

        if (playerId == 0 && isHatchingSlotOccupied) return false;
        if (playerId == 1 && opponentHatchingOccupied) return false;

        var eggDeck = (playerId == 0) ? player1Eggs : player2Eggs;

        if (eggDeck.Count == 0)
        {
            Debug.Log("No more eggs to hatch");
            return false;
        }

        int cardId = eggDeck[0];
        eggDeck.RemoveAt(0);

        HatchDigiegg(cardId, (ulong)playerId);

        if (playerId == 0) isHatchingSlotOccupied = true;
        else opponentHatchingOccupied = true;

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestHatchEggServerRpc(int playerId)
    {
        bool success = DrawCardFromEggs(playerId);

        if (!success)
        {
            HideEggImageClientRpc(playerId);
        }
    }

    [ClientRpc]
    private void HideEggImageClientRpc(int playerId)
    {
        if (playerId == 0)
        {
            Debug.Log("Hiding player Egg stack"); //TODO
        }
        else
        {
            Debug.Log("Hiding player 2 Egg stack");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetHatchingSlotServerRpc(int playerId)
    {
        if (playerId == 0)
            isHatchingSlotOccupied = false;
        else
            opponentHatchingOccupied = false;
    }

    public void PlayCardToBattleArea(Card card)
    {
        GlowManager.Instance.HideGlow();

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

        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            currentMemory -= cost;
        }
        else
        {
            currentMemory += cost;
        }

        if (card.cardType == "Digimon")
        {
            card.GetComponent<Image>().color = new Color32(0x7E, 0x7E, 0x7E, 0xFF);
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

            if (playerSecurityStackVisual.childCount > 0)
            {
                DestroyImmediate(playerSecurityStackVisual.GetChild(0).gameObject);
            }

            return topCard;
        }
        else if (playerId == 1 && player2SecurityStack.Count > 0)
        {
            int topCard = player2SecurityStack[0];
            player2SecurityStack.RemoveAt(0);

            if (opponentSecurityStackVisual.childCount > 0)
            {
                DestroyImmediate(opponentSecurityStackVisual.GetChild(0).gameObject);
            }

            return topCard;
        }

        return -1; // Invalid or empty
    }

    public void StartTurn()
    {
        if (isGameOver) return;

        DrawCardFromDeck(activePlayer.Value);

        if (isGameOver) return;

        player1SecurityBuff = 0;

        var allCards = FindObjectsOfType<Card>();
        foreach (var card in allCards)
        {
            if (card.ownerId == localPlayerId && (card.currentZone.Value == Card.Zone.BattleArea || card.currentZone.Value == Card.Zone.TamerArea) && !card.isDigivolved)
            {
                EffectManager.Instance.TriggerEffects(EffectTrigger.YourTurn, card);
            }
        }
    }

    public IEnumerator EndTurnServer()
    {
        var allCards = FindObjectsOfType<Card>();

        foreach (var card in allCards)
        {
            if (card.currentZone.Value == Card.Zone.BattleArea)
            {
                if (card.ownerId == localPlayerId)
                {
                    card.canAttack = true;
                    card.GetComponent<Image>().color = Color.white;

                    if (card.isSuspended)
                    {
                        card.isSuspended = false;
                        card.transform.rotation = Quaternion.identity;
                    }

                    card.ResetStats();
                }
                else
                {
                    card.isBlocking = false;
                }
            }

            if (card.currentZone.Value == Card.Zone.TamerArea && card.ownerId == localPlayerId)
            {
                card.mainEffectUsed = false;
            }

            BattleLogManager.Instance.ClearLog();

        }

        foreach (var card in FindObjectsOfType<Card>())
        {
            card.HideActionPanel();
        }

        turnTransition = true;

        endTurnBanner.SetActive(true);
        endTurnBanner.GetComponent<TurnTransitionBanner>().StartTransition();

        yield return new WaitForSeconds(2.5f);

        turnTransition = false;
        
        int nextPlayer = 0;

        if (activePlayer.Value == 0)
        {
            nextPlayer = 1;
        }

        activePlayer.Value = nextPlayer;
    }

    [ServerRpc]
    public void RequestEndTurnServerRpc()
    {
        StartCoroutine(EndTurnServer());
    }

    public void ForceEndTurn()
    {
        if (activePlayer.Value == localPlayerId)
        {
            currentMemory = 0;

            memoryManager.SetMemory(currentMemory);

            if (IsServer)
            {
                StartCoroutine(EndTurnServer());
            }
            else
            {
                RequestEndTurnServerRpc();
            }
        }
    }

    public void CheckTurnSwitch()
    {
        if ((NetworkManager.Singleton.LocalClientId == 0 && currentMemory < 0) ||
                (NetworkManager.Singleton.LocalClientId == 1 && currentMemory > 0))
        {
            if (IsServer)
            {
                StartCoroutine(EndTurnServer());
            }
            else
            {
                RequestEndTurnServerRpc();
            }
        }
    }

    public int GetActivePlayer()
    {
        return activePlayer.Value;
    }

    public IEnumerator ResolveSecurityAttack(Card attacker, int count, int dpBuff)
    {

        int opponentId = 0;

        if (attacker.ownerId == 0)
        {
            opponentId = 1;
        }

        for (int i = 0; i < count; i++)
        {
            turnTransition = true;
            if (attacker == null)
            {
                turnTransition = false;
                yield break;
            }

            Card blocker = FindObjectsOfType<Card>().FirstOrDefault(card => card.ownerId == opponentId && card.isBlocking && card.currentZone.Value == Card.Zone.BattleArea);

            if (blocker != null)
            {
                Debug.Log($"Blocker {blocker.cardName} intercepts the attack!");
                blocker.isBlocking = false;

                blocker.canAttack = false;
                blocker.GetComponent<Image>().color = new Color32(0x7E, 0x7E, 0x7E, 0xFF);
                blocker.isSuspended = true;
                blocker.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

                attacker.dpBuff = 0;
                EffectManager.Instance.TriggerEffects(EffectTrigger.WhenBlocked, attacker);

                int attackerDP_b = attacker.dp ?? 0;
                int blockerDP_b = blocker.dp ?? 0;

                attackerDP_b += dpBuff;
                attackerDP_b += attacker.dpBuff;

                Debug.Log($"Battle: Attacker DP {attackerDP_b} vs Blocker DP {blockerDP_b}");

                BattleLogManager.Instance.AddLog(
                            $"[Security Attack] {attacker.cardName}'s attack [{attackerDP_b} DP] is blocked by {blocker.cardName} [{blockerDP_b} DP]",
                            BattleLogManager.LogType.System,
                            attacker.ownerId);

                battlePreviewPanel.ShowPreview(attacker.sprite, blocker.sprite);

                yield return new WaitForSeconds(2f);

                if (attackerDP_b >= blockerDP_b)
                {
                    Debug.Log($"{blocker.cardName} is deleted!");

                    BattleLogManager.Instance.AddLog(
                            $"{blocker.cardName} is deleted!",
                            BattleLogManager.LogType.System,
                            attacker.ownerId);

                    if (blocker.ownerId == 0)
                    {
                        player1Trash.Add(blocker.cardId);
                    }
                    else
                    {
                        player2Trash.Add(blocker.cardId);
                    }
                    DestroyDigimonStack(blocker);
                }

                if (blockerDP_b >= attackerDP_b)
                {
                    Debug.Log($"{attacker.cardName} is deleted!");

                    BattleLogManager.Instance.AddLog(
                            $"{attacker.cardName} is deleted!",
                            BattleLogManager.LogType.System,
                            attacker.ownerId);

                    if (attacker.ownerId == 0)
                    {
                        player1Trash.Add(attacker.cardId);
                    }
                    else
                    {
                        player2Trash.Add(attacker.cardId);
                    }
                    DestroyDigimonStack(attacker);
                }

                battlePreviewPanel.HidePreview();
                turnTransition = false;
                yield break;
            }

            int securitycardId = RevealTopSecurityCard(opponentId);

            if (securitycardId == -1)
            {
                Debug.Log("Opponent has no security left, you win!");
                GameOver();
                yield break;
            }

            Debug.Log($"Revealed security card ID: {securitycardId}");

            if (idToSprite.TryGetValue(securitycardId, out Sprite sprite))
            {
                battlePreviewPanel.ShowPreview(attacker.sprite, sprite);
                yield return new WaitForSeconds(2f);
            }
            else
            {
                Debug.LogWarning("Sprite not found for Security card");
                turnTransition = false;
                yield break;
            }

            if (!idToData.TryGetValue(securitycardId, out CardData securityCardData))
            {
                Debug.LogWarning("Unable to fetch card data for revealed security card");
                turnTransition = false;
                yield break;
            }

            if (securityCardData.card_type == "Option" || securityCardData.card_type == "Tamer")
            {
                Debug.Log("Resolving Security Option effect.");

                GameObject cardGO = Instantiate(cardPrefab);
                var netObj = cardGO.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                {
                    netObj.SpawnWithOwnership(opponentId == 0 ? NetworkManager.Singleton.LocalClientId : GetRemoteClientId());
                }
                Card securityCard = cardGO.GetComponent<Card>();

                RectTransform rect = cardGO.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 220);

                securityCard.cardId = securityCardData.id;
                securityCard.cardName = securityCardData.name;
                securityCard.cardType = securityCardData.card_type;
                securityCard.ownerId = opponentId;
                securityCard.currentZone.Value = Card.Zone.None;
                securityCard.sprite = sprite;

                securityCard.effects = securityCardData.effects ?? new List<EffectData>();
                securityCard.inheritedEffects = securityCardData.inheritedEffects ?? new List<EffectData>();

                EffectManager.Instance.TriggerEffects(EffectTrigger.Security, securityCard);

                // After resolving, trash security card
                if (securityCard.currentZone.Value == Card.Zone.None)
                {
                    if (opponentId == 0)
                    {
                        player1Trash.Add(securitycardId);
                    }
                    else
                    {
                        player2Trash.Add(securitycardId);
                    }
                }
                battlePreviewPanel.HidePreview();
                turnTransition = false;
                yield break;
            }

            int attackerDP = attacker.dp ?? 0;
            int securityDP = securityCardData.dp ?? 0;

            attackerDP += dpBuff;

            if (opponentId == 0)
            {
                securityDP += player1SecurityBuff;
            }
            else
            {
                securityDP += player2SecurityBuff;
            }

            Debug.Log($"Attacker DP: {attackerDP} vs Security DP: {securityDP}");

            BattleLogManager.Instance.AddLog(
                            $"[Security Attack] {attacker.cardName} [{attackerDP} DP] is attacking {securityCardData.name} [{securityDP}]]",
                            BattleLogManager.LogType.System,
                            attacker.ownerId);

            if (securityDP >= attackerDP)
            {
                Debug.Log("Attacker is deleted.");

                BattleLogManager.Instance.AddLog(
                            $"{attacker.cardName} is deleted!",
                            BattleLogManager.LogType.System,
                            attacker.ownerId);

                if (attacker.ownerId == 0)
                {
                    player1Trash.Add(attacker.cardId);
                }
                else
                {
                    player2Trash.Add(attacker.cardId);
                }

                DestroyDigimonStack(attacker);
            }
            else
            {
                Debug.Log("Attacker won, not deleted from play.");

                BattleLogManager.Instance.AddLog(
                            $"{attacker.cardName} won!",
                            BattleLogManager.LogType.System,
                            attacker.ownerId);

                if (opponentId == 0)
                {
                    player1Trash.Add(securitycardId);
                }
                else
                {
                    player2Trash.Add(securitycardId);
                }
            }

            battlePreviewPanel.HidePreview();
            turnTransition = false;
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

        if (localPlayerId == 0)
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
        baseCard.isDigivolved = true;
        baseCard.GetComponent<CanvasGroup>().blocksRaycasts = true;
        newCard.GetComponent<CanvasGroup>().blocksRaycasts = true;

        newCard.inheritedStack.AddRange(baseCard.inheritedStack);
        newCard.inheritedStack.Add(baseCard);
        newCard.currentZone.Value = Card.Zone.BattleArea;
        newCard.GetComponent<Image>().color = new Color32(0x7E, 0x7E, 0x7E, 0xFF);

        Destroy(newCard.GetComponent<CardDropHandler>());

        if (baseCard.currentZone.Value == Card.Zone.BreedingActiveSlot)
        {
            newCard.GetComponent<CanvasGroup>().blocksRaycasts = false;

            if (baseCard.GetComponent<CardDropHandler>() == null)
            {
                baseCard.gameObject.AddComponent<CardDropHandler>();
            }
            baseCard.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        Debug.Log($"Successfully digivolved {baseCard.cardName} into {newCard.cardName}");

        BattleLogManager.Instance.AddLog(
                            $"Digivolved {baseCard.cardName} into {newCard.cardName}",
                            BattleLogManager.LogType.System,
                            baseCard.ownerId);

        EffectManager.Instance.TriggerEffects(EffectTrigger.WhenDigivolving, newCard);

        CheckTurnSwitch();

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDigivolveServerRpc(ulong baseCardId, ulong newCardId)
    {
        RunDigivolveEverywhereClientRpc(baseCardId, newCardId);
    }

    [ClientRpc]
    public void RunDigivolveEverywhereClientRpc(ulong baseCardId, ulong newCardId)
    {
        Card baseCard = NetworkManager.Singleton.SpawnManager.SpawnedObjects[baseCardId].GetComponent<Card>();
        Card newCard = NetworkManager.Singleton.SpawnManager.SpawnedObjects[newCardId].GetComponent<Card>();

        GameManager.Instance.TryDigivolve(baseCard, newCard);
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

        topCard.currentZone.Value = Card.Zone.BattleArea;

        Debug.Log("Promoted top card in digivolution stack to active.");

        PlayCardToBattleArea(topCard);
    }

    public void ModifyMemory(int delta)
    {
        if (localPlayerId == 0)
        {
            currentMemory += delta;
        }
        else
        {
            currentMemory -= delta;
        }

        currentMemory = Mathf.Clamp(currentMemory, -10, 10);
        memoryManager.SetMemory(currentMemory);
        CheckTurnSwitch();
    }

    public void SendToTrash(Card card)
    {
        if (card.ownerId == 0)
        {
            player1Trash.Add(card.cardId);
        }
        else
        {
            player2Trash.Add(card.cardId);
        }

        Destroy(card.gameObject);
    }

    public void DestroyDigimonStack(Card card)
    {
        foreach (var inheritedCard in card.inheritedStack)
        {
            if (inheritedCard != null)
            {
                if (inheritedCard.ownerId == 0)
                {
                    player1Trash.Add(inheritedCard.cardId);
                }
                else
                {
                    player2Trash.Add(inheritedCard.cardId);
                }
                Destroy(inheritedCard.gameObject);
            }
        }

        if (card.ownerId == 0)
        {
            player1Trash.Add(card.cardId);
        }
        else
        {
            player2Trash.Add(card.cardId);
        }

        Destroy(card.gameObject);
    }

    public void HideSecurityPreview()
    {
        Debug.Log("Triggering Hide Preview");
        battlePreviewPanel.HidePreview();
    }

    private ulong GetRemoteClientId()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) 
        {
            if (client.ClientId != NetworkManager.Singleton.LocalClientId)
            {
                return client.ClientId;
            }
        }
        return 0;
    }

    private EffectTrigger ParseTrigger(string trigger)
    {
        return trigger switch
        {
            "when_attacking" => EffectTrigger.WhenAttacking,
            "when_digivolving" => EffectTrigger.WhenDigivolving,
            "your_turn" => EffectTrigger.YourTurn,
            "main" => EffectTrigger.MainPhase,
            "when_blocked" => EffectTrigger.WhenBlocked,
            "security" => EffectTrigger.Security,
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
            "modify_ally_dp" => EffectType.ModifyAllyDP,
            "modify_party_dp" => EffectType.ModifyPartyDP,
            "modify_dp_child_count" => EffectType.ModifyDP_ChildCount,
            "gain_memory" => EffectType.GainMemory,
            "lose_memory" => EffectType.LoseMemory,
            "extra_security_attack" => EffectType.ExtraSecurityAttack,
            "increment_security_based_on_children" => EffectType.IncrementSecurityBasedOnChildren,
            "delete_target_opponent" => EffectType.DeleteTargetOpponent,
            "delete_opponent_dp_below_threshold" => EffectType.DeleteOpponentDPBelowThreshold,
            "buff_security_dp" => EffectType.BuffSecurityDP,
            "play_card_without_memory" => EffectType.PlayCardWithoutMemory,
            "extra_security_attack_party_next_turn" => EffectType.ExtraSecurityAttackPartyNextTurn,
            "buff_security_next_turn" => EffectType.BuffSecurityNextTurn,
            "activate_main_effect" => EffectType.ActivateMainEffect,
            _ => EffectType.None
        };
    }

}
