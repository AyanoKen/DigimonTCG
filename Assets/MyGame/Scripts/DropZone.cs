using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private Card.Zone zoneType;
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedCard = eventData.pointerDrag;

        if (droppedCard != null)
        {
            droppedCard.transform.SetParent(transform);

            Card card = droppedCard.GetComponent<Card>();

            if (card != null)
            {
                card.currentZone = zoneType;
            }
        }
    }
}
