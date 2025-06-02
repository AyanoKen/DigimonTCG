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
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            bool success = GameManager.Instance.DrawCardFromEggs();

            if (!success)
            {
                Color tempColor = eggImage.color;
                tempColor.a = 0f;
                eggImage.color = tempColor;
            }
        }
    }
}
