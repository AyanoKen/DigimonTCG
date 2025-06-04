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

            if (card.cardType == "Tamer")
            {
                if (zoneType != Card.Zone.TamerArea)
                {
                    Debug.Log("Tamer cards can only be played in the Tamer Area.");
                    return;
                }

                droppedCard.transform.SetParent(transform);
                card.currentZone = zoneType;
                GameManager.Instance.PlayCardToBattleArea(card); 
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
