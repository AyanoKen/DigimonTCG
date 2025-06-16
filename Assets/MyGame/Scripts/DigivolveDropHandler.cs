using UnityEngine;
using UnityEngine.EventSystems;

public class DigivolveDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObj = eventData.pointerDrag;

        if (draggedObj == null)
        {
            return;
        }

        Card draggedCard = draggedObj.GetComponent<Card>();
        Card targetCard = GetComponent<Card>();

        if (draggedCard.ownerId != GameManager.Instance.GetActivePlayer())
        {
            return;
        }

        if (targetCard.ownerId != GameManager.Instance.GetActivePlayer())
        {
            return;
        }

        if (targetCard.isDigivolved)
        {
            return;
        }

        if (targetCard.currentZone != Card.Zone.BattleArea && targetCard.currentZone != Card.Zone.BreedingActiveSlot)
            {
                Debug.Log("Can only evolve cards in battle area or breeding area");
                return;
            }

        GlowManager.Instance.HideGlow();
        bool success = GameManager.Instance.TryDigivolve(targetCard, draggedCard);
    }
}
