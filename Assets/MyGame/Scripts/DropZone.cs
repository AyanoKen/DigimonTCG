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
            Card card = droppedCard.GetComponent<Card>();

            if (card == null || card.ownerId != 0 || GameManager.Instance.GetActivePlayer() != 0)
            {
                return;
            }

            droppedCard.transform.SetParent(transform);

            if (card.currentZone == Card.Zone.BreedingActiveSlot)
            {
                GameManager.Instance.isHatchingSlotOccupied = false;
            }

            card.currentZone = zoneType;

            if (zoneType == Card.Zone.BattleArea)
            {
                GameManager.Instance.PlayCardToBattleArea(card);
            }
        }
    }
}
