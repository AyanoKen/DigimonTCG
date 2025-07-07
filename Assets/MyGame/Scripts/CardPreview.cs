using UnityEngine;
using UnityEngine.UI;

public class CardPreview : MonoBehaviour
{
    public int cardId;
    public Image cardImage;

    public void Setup(int id, Sprite sprite)
    {
        cardId = id;
        cardImage.sprite = sprite;
    }
}
