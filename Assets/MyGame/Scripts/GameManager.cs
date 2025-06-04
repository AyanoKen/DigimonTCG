using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json;

[System.Serializable]
public class CardData
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
    public string image_path;
    // TODO: Effect fields

    //Debug function to be able to log card data
    public override string ToString()
    {
        return $"ID: {id}, Name: {name}, Type: {card_type}, Level: {level}, PlayCost: {play_cost}, Color: {color}, Form: {form}, DP: {dp}";
    }
}

[System.Serializable]
public class CardDataList
{
    public List<CardData> cards;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handZone;
    [SerializeField] private Transform opponentHandZone;
    [SerializeField] private Transform breedingZone;
    [SerializeField] private MemoryGaugeManager memoryManager;
    [SerializeField] private Sprite cardBackSprite;

    private List<CardData> deckguide;
    private Dictionary<int, CardData> idToData = new Dictionary<int, CardData>();
    private Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();

    private List<int> digieggs = new List<int>();
    private List<int> deck = new List<int>();
    private List<int> player2Deck = new List<int>();
    private List<int> player2Hand = new List<int>();
    private List<int> player2Eggs = new List<int>();
    private int currentMemory = 0;

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
    }

    private void LoadDeckGuide()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/Agumon-Deck/AgumonDeckJSON");
        if (jsonFile == null)
        {
            Debug.LogError("Deck JSON file not found.");
            return;
        }

        deckguide = JsonConvert.DeserializeObject<List<CardData>>(jsonFile.text);

        foreach (CardData card in deckguide)
        {
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

        if (ownerId == localPlayerId)
        {
            if (idToSprite.TryGetValue(cardId, out Sprite sprite))
            {
                image.sprite = sprite;
            }
        }
        else
        {
            image.sprite = cardBackSprite;
        }


        Card card = cardGO.GetComponent<Card>();
        card.cardId = cardId;
        card.currentZone = Card.Zone.Hand;
        card.ownerId = ownerId;
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
    }

    public void DrawCardFromDeck()
    {
        if (deck.Count > 0)
        {
            int cardId = deck[0];
            deck.RemoveAt(0);
            SpawnCardToHand(cardId, handZone, 0);
        }
        else
        {
            Debug.Log("Deck is empty.");
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

        currentMemory -= cost;
        currentMemory = Mathf.Clamp(currentMemory, -10, 10);

        memoryManager.SetMemory(currentMemory);
    }

}
