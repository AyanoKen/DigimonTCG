using UnityEngine;
using UnityEngine.EventSystems;

public class CardDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private bool canDrag = false;
    private Card card;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        card = GetComponent<Card>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (card.currentZone == Card.Zone.Hand || card.currentZone == Card.Zone.BreedingActiveSlot)
        {
            canDrag = true;
            originalParent = transform.parent;
            transform.SetParent(transform.root);
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canDrag = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag)
        {
            return;
        }

        rectTransform.anchoredPosition += eventData.delta / transform.root.GetComponent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag)
        {
            return;
        }

        canvasGroup.blocksRaycasts = true;

        // If not dropped on a valid drop zone, return to original
        if (transform.parent == transform.root)
        {
            transform.SetParent(originalParent);
        }

        canDrag = false;
    }
}
