using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SecurityBattlePreview : MonoBehaviour
{
    [SerializeField] private Image attackerImage;
    [SerializeField] private Image defenderImage;

    public void ShowPreview(Sprite attacker, Sprite defender)
    {
        attackerImage.sprite = attacker;
        defenderImage.sprite = defender;
        gameObject.SetActive(true);
    }

    public void HidePreview()
    {
        gameObject.SetActive(false);
    }
}
