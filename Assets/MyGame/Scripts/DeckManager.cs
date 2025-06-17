using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    private List<CardData> deckguide = new List<CardData>();
    private Dictionary<int, CardData> idToData = new Dictionary<int, CardData>();
    private Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();

    public IReadOnlyDictionary<int, CardData> CardDatabase => idToData;
    public IReadOnlyDictionary<int, Sprite> SpriteDatabase => idToSprite;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void LoadDeck(string deckName)
    {
        deckguide.Clear();
        idToData.Clear();
        idToSprite.Clear();

        TextAsset jsonFile = Resources.Load<TextAsset>($"Cards/{deckName}/{deckName}");
        if (jsonFile == null)
        {
            Debug.LogError("Deck JSON file not found.");
            return;
        }

        JArray jsonArray = JArray.Parse(jsonFile.text);

        foreach (var token in jsonArray)
        {
            CardData card = token.ToObject<CardData>();
            deckguide.Add(card);
            idToData[card.id] = card;

            // Load Sprite
            string path = card.image_path.Replace("./", $"Cards/{deckName}/").Replace(".jpg", "").Replace(".png", "");
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
                idToSprite[card.id] = sprite;
            else
                Debug.LogWarning($"Image not found for: {path}");

            // Parse Effects
            ParseMainEffects(card, token);
            ParseInheritedEffects(card, token);
        }
    }

    private void ParseMainEffects(CardData card, JToken token)
    {
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
                    trigger = phase;

                string innerType = entry["effect"]?["type"]?.ToString();
                int value = entry["effect"]?["value"]?.Value<int>() ?? 0;
                int conditionValue = entry["effect"]?["conditionValue"]?.Value<int>() ?? 0;

                card.effects.Add(new EffectData(
                    ParserUtils.ParseTrigger(trigger),
                    ParserUtils.ParseEffectType(innerType ?? outerType, keyword),
                    value,
                    conditionValue
                ));
            }
        }
    }

    private void ParseInheritedEffects(CardData card, JToken token)
    {
        if (token["inherited_effect"] is JObject inh)
        {
            string phase = inh["phase"]?.ToString();
            string innerType = inh["effect"]?["type"]?.ToString();
            int value = inh["effect"]?["value"]?.Value<int>() ?? 0;
            int conditionValue = inh["effect"]?["conditionValue"]?.Value<int>() ?? 0;

            card.inheritedEffects = new List<EffectData>
            {
                new EffectData(
                    ParserUtils.ParseTrigger(phase),
                    ParserUtils.ParseEffectType(innerType),
                    value,
                    conditionValue
                )
            };
        }
    }

    public CardData GetCardData(int id)
    {
        return idToData.ContainsKey(id) ? idToData[id] : null;
    }

    public Sprite GetCardSprite(int id)
    {
        return idToSprite.ContainsKey(id) ? idToSprite[id] : null;
    }

    public List<int> GetMainDeckIds()
    {
        List<int> deck = new List<int>();
        foreach (var card in deckguide)
        {
            if (card.card_type != "Digi-Egg")
                deck.Add(card.id);
        }
        return deck;
    }

    public List<int> GetEggDeckIds()
    {
        List<int> eggs = new List<int>();
        foreach (var card in deckguide)
        {
            if (card.card_type == "Digi-Egg")
                eggs.Add(card.id);
        }
        return eggs;
    }
}
