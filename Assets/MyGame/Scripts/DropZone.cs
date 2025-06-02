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
}
