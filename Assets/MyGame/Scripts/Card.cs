using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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
    public bool canAttack = false;
    public GameObject actionPanel;

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
    public List<Card> inheritedStack = new List<Card>();
    public List<EffectData> effects = new List<EffectData>();
    public List<EffectData> inheritedEffects = new List<EffectData>();

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
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (transform.childCount > 1)
            {
                return;
            }

            Debug.Log($"Card {cardName} Able to detect click");
            if (currentZone == Zone.BattleArea && ownerId == GameManager.Instance.GetActivePlayer())
            {
                if (canAttack)
                {
                    Debug.Log($"Card {cardName} is ready to attack. Showing options...");

                    foreach (var card in FindObjectsOfType<Card>())
                    {
                        if (card != this)
                            card.actionPanel?.SetActive(false);
                    }

                    bool shouldShow = !actionPanel.activeSelf;
                    actionPanel.SetActive(shouldShow);
                }
                else
                {
                    Debug.Log("This Digimon cannot attack right now.");
                }
            }
        }
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

    public void AttackSecurity()
    {
        if (!canAttack || ownerId != GameManager.Instance.GetActivePlayer())
        {
            Debug.Log("Cannot Attack Security");
            return;
        }

        canAttack = false;
        actionPanel.SetActive(false);

        GameManager.Instance.ResolveSecurityAttack(this);
    }

    public void HideActionPanel()
    {
        actionPanel?.SetActive(false);
    }

    public bool CanDigivolveFrom(Card baseCard)
    {
        if (baseCard.level == null || this.level == null)
        {
            Debug.Log("Checkpoint 1");
            return false;
        }

        if (this.cardType != "Digimon")
        {
            Debug.Log("Checkpoint 2A: New card is not Digimon");
            return false;
        }

        if (baseCard.cardType == "Tamer" || baseCard.cardType == "Option")
        {
            Debug.Log("Checkpoint 2B: Cannot digivolve from Tamer/Option");
            return false;
        }

        if (this.level != baseCard.level + 1)
        {
            Debug.Log("Checkpoint 3");
            return false;
        }

        if (this.digivolveCost == null)
        {
            Debug.Log("Checkpoint 4");
            return false;
        }

        return this.digivolveCost.Any(entry => entry.color == baseCard.color);
    }

    public int GetDigivolveCost(Card baseCard)
    {
        var costEntry = this.digivolveCost.FirstOrDefault(entry => entry.color == baseCard.color);
        return costEntry != null ? costEntry.cost : -1;
    }
}
