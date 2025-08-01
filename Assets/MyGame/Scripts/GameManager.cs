/*
    GameManager.cs
    

*/

using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Netcode; 
using UnityEngine.SceneManagement;

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
    [SerializeField] private Transform playerSecurityStackVisual;
    [SerializeField] private Transform opponentSecurityStackVisual;
    [SerializeField] private MemoryGaugeManager memoryManager;
    [SerializeField] public Sprite cardBackSprite;
    [SerializeField] private GameObject securityRevealPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private GameObject endTurnBanner;
    [SerializeField] private SecurityBattlePreview battlePreviewPanel;
    [SerializeField] private GameObject GameEndScreen;
    [SerializeField] private TMPro.TMP_Text resultText;
    [SerializeField] private GameObject joinCodePanel;
    [SerializeField] private TMPro.TMP_Text joinCodeText;


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
    public Dictionary<int, CardData> idToData = new Dictionary<int, CardData>();
    public Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();

    private List<int> player1Eggs = new List<int>();
    private List<int> player2Eggs = new List<int>();
    private List<int> player1Deck = new List<int>();
    private List<int> player2Deck = new List<int>();
    private List<int> player1SecurityStack = new List<int>();
    private List<int> player2SecurityStack = new List<int>();
    private List<int> player1Trash = new List<int>();
    private List<int> player2Trash = new List<int>();

    private bool isGameOver = false;
    public bool isHatchingSlotOccupied = false;
    public bool opponentHatchingOccupied = false;
    public int localPlayerId = 0;
    public int player1SecurityBuff = 0;
    public int player2SecurityBuff = 0;
    

    public struct SecurityAttackResult // Datatype to interpret security attack details
    {
        public ulong attackerNetId;
        public ulong blockerNetId;
        public int attackerDP;
        public int opponentDP;
        public int revealedCardId;
        public bool attackerDeleted;
        public bool blockerDeleted;
        public bool wasBlocked;
        public bool isOptionOrTamer;
    }
    
    public NetworkVariable<int> currentMemory = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> turnTransition = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> activePlayer = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        Instance = this;
        currentMemory.Value = memoryManager.GetCurrentMemory();
    }

    private void Start()
    {
        LoadDeckGuide();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        if (IsHost && joinCodePanel != null && joinCodeText != null)
        {
            joinCodePanel.SetActive(true);
            joinCodeText.text = $"Join Code: {RelaySessionInfo.JoinCode}";
        }
    }

    void Update()
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log("Host disconnected. Returning to lobby...");
            SceneManager.LoadScene("Lobby");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            Debug.Log("Both players connected. Starting game.");

            if (joinCodePanel != null)
            {
                Destroy(joinCodePanel);
            }

            InitializeDeck();
            DrawStartingHands(5);
            InitializeSecurityStacks();
            StartTurn();

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
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

        if (IsServer)
        {
            turnTransition.Value = false;
        }

        if ((ulong)current == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("It's my turn!");

                var allCards = FindObjectsOfType<Card>();
                foreach (var card in allCards)
                {
                    // Trigger start of the turn effects of active cards in the field
                    if (card.ownerId == localPlayerId && (card.currentZone.Value == Card.Zone.BattleArea ||
                        card.currentZone.Value == Card.Zone.TamerArea) && !card.isDigivolved)
                    {
                        EffectManager.Instance.TriggerEffects(EffectTrigger.YourTurn, card);
                    }
                }

                StartTurn();
            }
            else
            {
                Debug.Log("It's opponent's turn.");

                var allCards = FindObjectsOfType<Card>();
                foreach (var card in allCards)
                {
                    if (card.ownerId != localPlayerId && (card.currentZone.Value == Card.Zone.BattleArea ||
                        card.currentZone.Value == Card.Zone.TamerArea) && !card.isDigivolved)
                    {
                        EffectManager.Instance.TriggerEffects(EffectTrigger.YourTurn, card);
                    }
                }
            }
    }

    [ClientRpc]
    public void GameOverClientRpc(int winner) //Runs after game ends, to announce results
    {
        Debug.Log("Game Over");

        if (winner == localPlayerId)
        {
            resultText.text = "You Won :)";
        }
        else
        {
            resultText.text = "You Lost :(";
        }

        GameEndScreen.SetActive(true);
    }

    public void ReturnToLobby() //TODO: Weird edge case bug with this function
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    }

    private void LoadDeckGuide() //Converts card json data to machine readable data structure
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/MasterDeckJSON");
        if (jsonFile == null)
        {
            Debug.LogError("Deck JSON file not found.");
            return;
        }

        JArray jsonArray = JArray.Parse(jsonFile.text);
        deckguide = new List<CardData>();

        List<int> allEggCardIds = new List<int>();
        List<int> allPlayableCardIds = new List<int>();

        foreach (var token in jsonArray)
        {
            CardData card = token.ToObject<CardData>();
            deckguide.Add(card);
            idToData[card.id] = card;

            // Load and cache image
            string path = "Cards" + card.image_path.Replace("./", "/").Replace(".jpg", "").Replace(".png", "");
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                idToSprite[card.id] = sprite;
            }
            else
            {
                Debug.LogWarning("Image not found for: " + path);
            }

            if (card.card_type == "Digi-Egg")
            {
                allEggCardIds.Add(card.id);
            }
            else
            {
                allPlayableCardIds.Add(card.id);
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

        //The below makes sure that each deck contains exactly 5 egg cards and 45 regular cards
        player1Eggs = GetRandomCardsWithRepeat(allEggCardIds, 5);
        player2Eggs = GetRandomCardsWithRepeat(allEggCardIds, 5);

        player1Deck = GetRandomCardsWithRepeat(allPlayableCardIds, 45);
        player2Deck = GetRandomCardsWithRepeat(allPlayableCardIds, 45);

        Debug.Log($"[LoadDeckGuide] Loaded {deckguide.Count} cards");
        Debug.Log($"[Deck Split] Player1 Eggs: {player1Eggs.Count}, Player1 Deck: {player1Deck.Count}");
        Debug.Log($"[Deck Split] Player2 Eggs: {player2Eggs.Count}, Player2 Deck: {player2Deck.Count}");
    }

    //Helper function to create egg and regular decks
    private List<int> GetRandomCardsWithRepeat(List<int> source, int count)
    {
        if (source == null || source.Count == 0)
        {
            Debug.LogError("Source list is empty. Cannot pick cards.");
            return new List<int>();
        }

        List<int> result = new List<int>();

        while (result.Count < count)
        {
            var shuffled = source.OrderBy(x => Random.value).ToList();
            foreach (var id in shuffled)
            {
                result.Add(id);
                if (result.Count == count)
                    break;
            }
        }

        return result;
    }

    private void InitializeDeck() //Randomize the decks of each player
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestCardDrawServerRpc(int playerId)
    {
        DrawCardFromDeck(playerId);
    }

    //Draw card from Deck depending on player
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
        card.cardId.Value = cardId;
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
        card.cardId.Value = cardId;
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
            card.digivolveCost = data.digivolve_costs?.Select(dc => new DigivolveCostEntry { color = dc.color, cost = dc.cost }).ToList() ?? new List<DigivolveCostEntry>();
            card.effects = data.effects ?? new List<EffectData>();
            card.inheritedEffects = data.inheritedEffects ?? new List<EffectData>();

            card.InitializeFlagsFromEffects();
        }

        card.RemoveDropHandlerClientRpc(); //Makes the egg card be non interactable until evolved
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

        if (!idToData.ContainsKey(card.cardId.Value))
        {
            Debug.LogError("Card ID Missing in database");
        }

        CardData data = idToData[card.cardId.Value];

        int cost = 0;

        if (data.play_cost.HasValue)
        {
            cost = data.play_cost.Value;
        }

        ModifyMemoryServerRpc(cost);

        if (card.cardType == "Digimon") //Card inactive visual cue
        {
            card.GetComponent<Image>().color = new Color32(0x7E, 0x7E, 0x7E, 0xFF);
        }
    }

    public int RevealTopSecurityCard(int playerId)
    {
        if (playerId == 0 && player1SecurityStack.Count > 0)
        {
            int topCard = player1SecurityStack[0];
            player1SecurityStack.RemoveAt(0);

            UpdateSecurityStackClientRpc(playerId);

            return topCard;
        }
        else if (playerId == 1 && player2SecurityStack.Count > 0)
        {
            int topCard = player2SecurityStack[0];
            player2SecurityStack.RemoveAt(0);

            UpdateSecurityStackClientRpc(playerId);

            return topCard;
        }

        return -1; // Invalid or empty
    }

    [ClientRpc]
    public void UpdateSecurityStackClientRpc(int playerId)
    {
        //Updates the visual stack of security cards in the game view
        if (playerId == localPlayerId)
        {
            if (playerSecurityStackVisual.childCount > 0)
            {
                DestroyImmediate(playerSecurityStackVisual.GetChild(0).gameObject);
            }
        }
        else
        {
            if (opponentSecurityStackVisual.childCount > 0)
            {
                DestroyImmediate(opponentSecurityStackVisual.GetChild(0).gameObject);
            }
        }
    }

    public void StartTurn()
    {
        if (isGameOver) return;

        if (IsServer)
        {
            DrawCardFromDeck(activePlayer.Value);
            player1SecurityBuff = 0;
        }
        else
        {
            RequestCardDrawServerRpc(localPlayerId);
            player2SecurityBuff = 0;
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestEndTurnServerRpc()
    {
        Debug.Log($"[TurnRPC] Server received RequestEndTurnServerRpc from player {activePlayer.Value}");

        turnTransition.Value = true;

        RunEndTurnClientRpc(activePlayer.Value);
    }

    [ClientRpc]
    public void RunEndTurnClientRpc(int prevPlayer)
    {
        StartCoroutine(EndTurn(prevPlayer));
    }

    public IEnumerator EndTurn(int prevPlayerId)
    {
        yield return new WaitForSeconds(0.2f);

        var allCards = FindObjectsOfType<Card>();

        foreach (var card in allCards)
        {
            if (card.currentZone.Value == Card.Zone.BattleArea)
            {
                if (card.ownerId == prevPlayerId)
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

            if (card.currentZone.Value == Card.Zone.TamerArea && card.ownerId == prevPlayerId)
            {
                card.mainEffectUsed = false;
            }

        }

        foreach (var card in FindObjectsOfType<Card>())
        {
            card.HideActionPanel();
        }

        endTurnBanner.SetActive(true);
        endTurnBanner.GetComponent<TurnTransitionBanner>().StartTransition();

        yield return new WaitForSeconds(2.5f);

        int nextPlayer = 0;

        if (activePlayer.Value == 0)
        {
            nextPlayer = 1;
        }

        if (IsServer)
        {
            activePlayer.Value = nextPlayer;
        }

        BattleLogManager.Instance.AddLog("--------------------X--End Of Turn--X--------------------", Color.cyan);
    }


    public void ForceEndTurn() //Trigger end turn on demand
    {
        if (activePlayer.Value == localPlayerId && turnTransition.Value == false)
        {
            ResetMemoryServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetMemoryServerRpc()
    {
        currentMemory.Value = 0;

        memoryManager.SetMemory(currentMemory.Value);

        RequestEndTurnServerRpc();
    }

    public void CheckTurnSwitch()
    {
        if ((activePlayer.Value == 0 && currentMemory.Value < 0) ||
                (activePlayer.Value == 1 && currentMemory.Value > 0))
        {
            RequestEndTurnServerRpc();
        }
    }

    public int GetActivePlayer()
    {
        return activePlayer.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSecurityAttackServerRpc(ulong attackerId, int attackCount, int dpBuff)
    {
        Card attacker = NetworkManager.Singleton.SpawnManager.SpawnedObjects[attackerId].GetComponent<Card>();
        if (attacker == null) return;

        var results = ResolveSecurityAttack(attacker, attackCount, dpBuff);

        foreach (var r in results)
        {
            PropagateSecurityResultClientRpc(
                r.attackerNetId,
                r.blockerNetId,
                r.attackerDP,
                r.opponentDP,
                r.revealedCardId,
                r.attackerDeleted,
                r.blockerDeleted,
                r.wasBlocked,
                r.isOptionOrTamer
            );
        }
    }

    [ClientRpc]
    public void PropagateSecurityResultClientRpc(
        ulong attackerId,
        ulong blockerId,
        int attackerDP,
        int opponentDP,
        int revealedCardId,
        bool attackerDeleted,
        bool blockerDeleted,
        bool wasBlocked,
        bool isOptionOrTamer)
    {
        
        Card attacker = GetCard(attackerId);
        Card blocker = GetCard(blockerId);

        if (revealedCardId == -1)
        {
            Debug.Log("Opponent has no security left, you win!");

            if (IsServer)
            {
                GameOverClientRpc(attacker.ownerId);
            }

            return;
        }

        Sprite revealedSprite = null;

        if (wasBlocked)
        {
            revealedSprite = blocker.sprite;

            Debug.Log($"Blocker {blocker.cardName} intercepts the attack!");
            blocker.isBlocking = false;

            blocker.canAttack = false;
            blocker.GetComponent<Image>().color = new Color32(0x7E, 0x7E, 0x7E, 0xFF);
            blocker.isSuspended = true;

            Debug.Log($"Battle: Attacker DP {attackerDP} vs Blocker DP {opponentDP}");

            BattleLogManager.Instance.AddLog(
                        $"[Security Attack] {attacker.cardName}'s attack [{attackerDP} DP] is blocked by {blocker.cardName} [{opponentDP} DP]",
                        BattleLogManager.LogType.System,
                        attacker.ownerId);

        }
        else
        {
            idToSprite.TryGetValue(revealedCardId, out revealedSprite);

            BattleLogManager.Instance.AddLog(
                        $"[Security Attack] {attacker.cardName} [{attackerDP} DP] is attacking opponent's security [{opponentDP} DP]",
                        BattleLogManager.LogType.System,
                        attacker.ownerId);
        }

        if (attackerDeleted)
        {
            Debug.Log($"{attacker.cardName} is deleted!");

            BattleLogManager.Instance.AddLog(
                    $"{attacker.cardName} is deleted!",
                    BattleLogManager.LogType.System,
                    attacker.ownerId);
            
            DestroyDigimonStack(attacker);
        }
        else
        {
            Debug.Log("Opponent's card is deleted!");

            BattleLogManager.Instance.AddLog(
                    "Opponent's card is deleted!",
                    BattleLogManager.LogType.System,
                    attacker.ownerId);   
        }

        if (blockerDeleted)
        {
            DestroyDigimonStack(blocker);
        }

        battlePreviewPanel.ShowPreview(attacker.sprite, revealedSprite);

        StartCoroutine(HidePreviewAfterDelay());
    }

    private IEnumerator HidePreviewAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        battlePreviewPanel.HidePreview();
    }
    
    private Card GetCard(ulong netId)
    {
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var obj)
            ? obj.GetComponent<Card>()
            : null;
    }


    private List<SecurityAttackResult> ResolveSecurityAttack(Card attacker, int count, int dpBuff)
    {
        List<SecurityAttackResult> results = new List<SecurityAttackResult>();

        int opponentId = 0;

        if (attacker.ownerId == 0)
        {
            opponentId = 1;
        }

        for (int i = 0; i < count; i++)
        {
            SecurityAttackResult result = new SecurityAttackResult
            {
                attackerNetId = attacker.NetworkObjectId,
                blockerNetId = 0,
                attackerDP = 0,
                opponentDP = 0,
                revealedCardId = -2,
                attackerDeleted = false,
                blockerDeleted = false,
                wasBlocked = false,
                isOptionOrTamer = false
            };

            Card blocker = FindObjectsOfType<Card>().FirstOrDefault(card => card.ownerId == opponentId && card.isBlocking && card.currentZone.Value == Card.Zone.BattleArea);

            if (blocker != null)
            {
                result.wasBlocked = true;
                result.revealedCardId = -2;
                result.blockerNetId = blocker.NetworkObjectId;

                blocker.isBlocking = false;

                attacker.dpBuff = 0;
                EffectManager.Instance.TriggerEffects(EffectTrigger.WhenBlocked, attacker);

                int attackerDP_b = attacker.dp ?? 0;
                int blockerDP_b = blocker.dp ?? 0;

                attackerDP_b += dpBuff;
                attackerDP_b += attacker.dpBuff;

                result.attackerDP = attackerDP_b;
                result.opponentDP = blockerDP_b;

                if (attackerDP_b >= blockerDP_b)
                {

                    if (blocker.ownerId == 0)
                    {
                        player1Trash.Add(blocker.cardId.Value);
                    }
                    else
                    {
                        player2Trash.Add(blocker.cardId.Value);
                    }
                    result.blockerDeleted = true;
                }

                if (blockerDP_b >= attackerDP_b)
                {

                    if (attacker.ownerId == 0)
                    {
                        player1Trash.Add(attacker.cardId.Value);
                    }
                    else
                    {
                        player2Trash.Add(attacker.cardId.Value);
                    }
                    result.attackerDeleted = true;

                    results.Add(result);
                    return results;
                }

                results.Add(result);
                continue;
            }

            int securitycardId = RevealTopSecurityCard(opponentId);
            result.revealedCardId = securitycardId;

            if (securitycardId == -1)
            {
                results.Add(result);
                return results;
            }

            if (!idToSprite.TryGetValue(securitycardId, out Sprite sprite))
            {
                Debug.LogWarning("Sprite not found for Security card");
                continue;
            }

            if (!idToData.TryGetValue(securitycardId, out CardData securityCardData))
            {
                Debug.LogWarning("Unable to fetch card data for revealed security card");
                continue;
            }

            if (securityCardData.card_type == "Option" || securityCardData.card_type == "Tamer")
            {

                GameObject cardGO = Instantiate(cardPrefab); //TODO: Destroy this card if its not spawned
                var netObj = cardGO.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                {
                    netObj.SpawnWithOwnership(opponentId == 0 ? NetworkManager.Singleton.LocalClientId : GetRemoteClientId());
                }
                Card securityCard = cardGO.GetComponent<Card>();

                RectTransform rect = cardGO.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 220);

                securityCard.cardId.Value = securityCardData.id;
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

                results.Add(result);
                continue;
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

            result.attackerDP = attackerDP;
            result.opponentDP = securityDP;

            if (securityDP >= attackerDP)
            {

                if (attacker.ownerId == 0)
                {
                    player1Trash.Add(attacker.cardId.Value);
                }
                else
                {
                    player2Trash.Add(attacker.cardId.Value);
                }

                result.attackerDeleted = true;
                results.Add(result);
                return results;
            }
            else
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

            results.Add(result);
        }

        return results;
    }

    public bool TryDigivolve(Card baseCard, Card newCard)
    {
        if (!newCard.CanDigivolveFrom(baseCard))
        {
            return false;
        }

        int cost = newCard.GetDigivolveCost(baseCard);

        if (cost < 0)
        {
            return false;
        }

        newCard.transform.SetParent(baseCard.transform, false);
        newCard.transform.localPosition = new Vector3(0, 30f, 0);
        baseCard.isDigivolved = true;
        baseCard.GetComponent<CanvasGroup>().blocksRaycasts = true;
        newCard.GetComponent<CanvasGroup>().blocksRaycasts = true;

        newCard.inheritedStack.AddRange(baseCard.inheritedStack);
        newCard.inheritedStack.Add(baseCard);
        if (IsServer)
        {
            newCard.currentZone.Value = Card.Zone.BattleArea;
        }
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
                            $"Player {baseCard.ownerId} Digivolved {baseCard.cardName} into {newCard.cardName}",
                            BattleLogManager.LogType.System,
                            baseCard.ownerId);

        EffectManager.Instance.TriggerEffects(EffectTrigger.WhenDigivolving, newCard);

        if (IsServer)
        {
            ModifyMemoryServerRpc(cost);
        }

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

        stackBase.transform.parent = null;
        topCard.currentZone.Value = Card.Zone.BattleArea;

        stackBase.NotifyZoneChange(Card.Zone.BattleArea);

        PlayCardToBattleArea(topCard);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ModifyMemoryServerRpc(int cost)
    {
        if (activePlayer.Value == 0)
        {
            currentMemory.Value -= cost;
        }
        else
        {
            currentMemory.Value += cost;
        }

        currentMemory.Value = Mathf.Clamp(currentMemory.Value, -10, 10);
        SetMemoryClientRpc(currentMemory.Value);

        CheckTurnSwitch();
    }

    [ClientRpc]
    private void SetMemoryClientRpc(int newValue)
    {
        memoryManager.SetMemory(newValue);
    }

    public void SendToTrash(Card card)
    {
        if (card.ownerId == 0)
        {
            player1Trash.Add(card.cardId.Value);
        }
        else
        {
            player2Trash.Add(card.cardId.Value);
        }

        Destroy(card.gameObject);
    }

    public void DestroyDigimonStack(Card card)
    {
        if (IsServer)
        {
            foreach (var inheritedCard in card.inheritedStack)
            {
                if (inheritedCard != null)
                {
                    if (inheritedCard.ownerId == 0)
                    {
                        player1Trash.Add(inheritedCard.cardId.Value);
                    }
                    else
                    {
                        player2Trash.Add(inheritedCard.cardId.Value);
                    }
                    Destroy(inheritedCard.gameObject);
                }
            }

            if (card.ownerId == 0)
            {
                player1Trash.Add(card.cardId.Value);
            }
            else
            {
                player2Trash.Add(card.cardId.Value);
            }

            Destroy(card.gameObject);
        } 
    }

    public void HideSecurityPreview()
    {
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestAnnoucementServerRpc(string log, int playerId)
    {
        RunAnnouncementClientRpc(log, playerId);
    }

    [ClientRpc]
    public void RunAnnouncementClientRpc(string log, int playerId)
    {
        BattleLogManager.Instance.AddLog(
                            log,
                            BattleLogManager.LogType.System,
                            playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ModifySecurityBuffServerRpc(int buffValue, int playerId)
    {
        if (localPlayerId == playerId)
        {
            player1SecurityBuff += buffValue;
        }
        else
        {
            player2SecurityBuff += buffValue;
        }
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
