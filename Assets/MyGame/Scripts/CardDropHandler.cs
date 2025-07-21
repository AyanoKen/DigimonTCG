using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
        if (GameManager.Instance.GetActivePlayer() == GameManager.Instance.localPlayerId && card.ownerId == GameManager.Instance.localPlayerId &&
            (card.currentZone.Value == Card.Zone.Hand || card.currentZone.Value == Card.Zone.BreedingActiveSlot) && 
            GameManager.Instance.turnTransition == false)
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

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Transform hoveredTarget = null;

        foreach (var result in results)
        {
            var digimonCard = result.gameObject.GetComponent<Card>();
            if (digimonCard != null && digimonCard != card && card.CanDigivolveFrom(digimonCard) && digimonCard.ownerId == card.ownerId && !digimonCard.isDigivolved)
            {
                hoveredTarget = digimonCard.transform;
                break;
            }

            var dropZone = result.gameObject.GetComponent<DropZone>();
            if (dropZone != null && hoveredTarget == null)
            {
                hoveredTarget = dropZone.transform;
            }
        }

        if (hoveredTarget != null)
        {
            GlowManager.Instance.ShowGlow(hoveredTarget);
        }
        else
        {
            GlowManager.Instance.HideGlow();
        }
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

        GlowManager.Instance.HideGlow();
        canDrag = false;

    }
}
