using UnityEngine;
using UnityEngine.EventSystems;

public class DeckZone : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            GameManager.Instance.DrawCardFromDeck();
        }
    }
}
