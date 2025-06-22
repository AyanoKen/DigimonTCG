using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EggStack : MonoBehaviour, IPointerClickHandler
{
    private Image eggImage;

    private void Awake()
    {
        eggImage = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && GameManager.Instance.GetActivePlayer() == GameManager.Instance.localPlayerId)
        {
            GameManager.Instance.RequestHatchEggServerRpc(GameManager.Instance.localPlayerId);
        }
    }
}
