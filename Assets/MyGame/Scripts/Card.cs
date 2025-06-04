using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class DigivolveCostEntry
{
    public string color;
    public int cost;
}


public class Card : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public int cardId;
    public Zone currentZone;
    public int ownerId;

    public enum Zone
    {
        Deck,
        Hand,
        BreedingEggSlot,
        BreedingActiveSlot,
        BattleArea,
        TamerArea,
        Trash,
        Security
    }

    public string cardName;
    public string cardType;
    public int? level;
    public string color;
    public string form;
    public string attribute;
    public int? dp;
    public int playCost;
    public Sprite sprite;
    public List<DigivolveCostEntry> digivolveCost;

    private bool isZoomed = false;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Vector2 originalSize;
    private Transform originalParent;
    private Canvas topCanvas;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Intentionally left empty to avoid conflicts with hold detection
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ZoomIn();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ZoomOut();
        }
    }

    private void ZoomIn()
    {
        if (isZoomed) return;

        isZoomed = true;
        originalPosition = transform.position;
        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalSize = rectTransform.sizeDelta;

        topCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        transform.SetParent(topCanvas.transform, true);
        transform.SetAsLastSibling();

        Vector2 centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        transform.position = centerScreen;

        // Set fixed zoom size
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 450);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 660);
    }

    private void ZoomOut()
    {
        if (!isZoomed) return;

        isZoomed = false;
        transform.SetParent(originalParent, true);
        transform.localScale = originalScale;
        transform.position = originalPosition;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalSize.y);
    }
}
