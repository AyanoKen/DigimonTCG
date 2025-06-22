using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

/*Script handles what happens when a digimon card is dropped onto another digimon card

In other words, digivolution logic.*/

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

        if (GameManager.Instance.GetActivePlayer() != GameManager.Instance.localPlayerId)
        {
            return;
        }

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

        if (targetCard.currentZone.Value != Card.Zone.BattleArea && targetCard.currentZone.Value != Card.Zone.BreedingActiveSlot)
        {
            Debug.Log("Can only evolve cards in battle area or breeding area");
            return;
        }

        GlowManager.Instance.HideGlow();
        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.RunDigivolveEverywhereClientRpc(
                targetCard.NetworkObjectId, draggedCard.NetworkObjectId);
        }
        else
        {
            GameManager.Instance.RequestDigivolveServerRpc(
                targetCard.NetworkObjectId, draggedCard.NetworkObjectId);
        }
    }
}
