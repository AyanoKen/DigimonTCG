using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

public class DeckManager : MonoBehaviour
{
    public GameObject cardPreviewPrefab;
    public Transform contentPanel;
    public string deckJsonPath = "Cards/Agumon-Deck/AgumonDeckJSON";
    public Image zoomPreviewImage;

    private Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();

    void Start()
    {
        LoadAndDisplayDeck();
    }

    private void LoadAndDisplayDeck()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(deckJsonPath);

        if (jsonFile == null)
        {
            Debug.LogError("Deck JSON not found at path: " + deckJsonPath);
            return;
        }

        JArray jsonArray = JArray.Parse(jsonFile.text);

        foreach (var token in jsonArray)
        {
            int id = token["id"]?.Value<int>() ?? -1;
            string imagePath = token["image_path"]?.ToString();

            if (string.IsNullOrEmpty(imagePath))
                continue;

            string path = imagePath.Replace("./", "Cards/Agumon-Deck/").Replace(".jpg", "").Replace(".png", "");

            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("Missing sprite for path: " + path);
                continue;
            }

            idToSprite[id] = sprite;

            GameObject cardGO = Instantiate(cardPreviewPrefab, contentPanel);
            CardPreview viewer = cardGO.GetComponent<CardPreview>();
            viewer.Setup(id, sprite);
            viewer.AssignManager(this); 
        }
    }
    
    public void SetZoomPreview(Sprite sprite)
    {
        if (zoomPreviewImage != null)
        {
            zoomPreviewImage.sprite = sprite;
        }
    }
}
