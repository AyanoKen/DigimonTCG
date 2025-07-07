using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardPreview : MonoBehaviour, IPointerClickHandler
{
    public int cardId;
    public Image cardImage;

    private Sprite sprite;

    private DeckManager deckManager;

    public void Setup(int id, Sprite sprite)
    {
        cardId = id;
        this.sprite = sprite;
        cardImage.sprite = sprite;
    }

    public void AssignManager(DeckManager manager)
    {
        deckManager = manager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("huh");
        if (deckManager != null)
        {
            deckManager.SetZoomPreview(sprite);
        }
        else
        {
            Debug.LogWarning("DeckManager not found in scene.");
        }
    }
}
