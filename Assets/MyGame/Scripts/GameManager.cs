using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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

    private List<CardData> deckguide;
    private Dictionary<int, CardData> idToData = new Dictionary<int, CardData>();
    private Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();

    private List<int> digieggs = new List<int>();
    private List<int> deck = new List<int>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadDeckGuide();
        InitializeDeck();
        DrawStartingHand(5);
    }

    private void LoadDeckGuide()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/Agumon-Deck/AgumonDeckJSON");
        if (jsonFile == null)
        {
            Debug.LogError("Deck JSON file not found.");
            return;
        }

        string json = "{\"cards\":" + jsonFile.text + "}"; // Wrap in object for parsing
        CardDataList wrapper = JsonUtility.FromJson<CardDataList>(json);

        deckguide = wrapper.cards;

        foreach (CardData card in deckguide)
        {
            idToData[card.id] = card;

            if (card.card_type == "Digi-Egg")
                digieggs.Add(card.id);
            else
                deck.Add(card.id);

            // Load and cache the image
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

    private void DrawStartingHand(int count)
    {
        for (int i = 0; i < count && deck.Count > 0; i++)
        {
            int cardId = deck[0];
            deck.RemoveAt(0);
            SpawnCardToHand(cardId);
        }
    }

    private void SpawnCardToHand(int cardId)
    {
        GameObject cardGO = Instantiate(cardPrefab, handZone);
        Image image = cardGO.GetComponent<Image>();

        if (idToSprite.TryGetValue(cardId, out Sprite sprite))
        {
            image.sprite = sprite;
        }

        Card card = cardGO.GetComponent<Card>();
        card.cardId = cardId;
        card.currentZone = Card.Zone.Hand;
    }

    public void DrawCardFromDeck()
{
    if (deck.Count > 0)
    {
        int cardId = deck[0];
        deck.RemoveAt(0);
        SpawnCardToHand(cardId);
    }
    else
    {
        Debug.Log("Deck is empty.");
    }
}
}
