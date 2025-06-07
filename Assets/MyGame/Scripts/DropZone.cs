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

                card.GetComponent<CanvasGroup>().blocksRaycasts = true;
                Destroy(card.GetComponent<CardDropHandler>());

                GameManager.Instance.PlayCardToBattleArea(card);
                return;
            }

            if (zoneType == Card.Zone.TamerArea)
            {
                Debug.Log("Only Tamer cards can be played into the tamer area");
                return;
            }

            droppedCard.transform.SetParent(transform);

            if (card.currentZone == Card.Zone.BreedingActiveSlot)
            {
                GameManager.Instance.isHatchingSlotOccupied = false;
                GameManager.Instance.PromoteDigivolvedCardToBattle(card);
                return;
            }

            card.currentZone = zoneType;

            card.GetComponent<CanvasGroup>().blocksRaycasts = true;
            Destroy(card.GetComponent<CardDropHandler>());

            GameManager.Instance.PlayCardToBattleArea(card);
        }
    }
}
