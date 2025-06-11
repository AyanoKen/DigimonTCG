using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TriggerEffects(EffectTrigger trigger, Card card)
    {
        if (card.effects != null)
        {
            foreach (var effect in card.effects)
            {
                if (effect.trigger == trigger)
                {
                    ApplyEffect(effect, card);
                }
            }
        }

        foreach (var inheritedCard in card.inheritedStack)
        {
            if (inheritedCard.inheritedEffects != null)
            {
                foreach (var effect in inheritedCard.inheritedEffects)
                {
                    if (effect.trigger == trigger)
                    {
                        ApplyEffect(effect, card);
                    }
                }
            }
        }
    }

    private void ApplyEffect(EffectData effect, Card source)
    {
        switch (effect.type)
        {
            case EffectType.ModifyDP:
                {
                    source.dpBuff += effect.value;
                    Debug.Log($"[Effect] {source.cardName} gains {effect.value} DP.");
                    break;
                }

            case EffectType.GainMemory:
                {
                    GameManager.Instance.ModifyMemory(source.ownerId == 0 ? effect.value : -effect.value);
                    Debug.Log($"[Effect] {source.cardName} gains {effect.value} memory.");
                    break;
                }

            case EffectType.LoseMemory:
                {
                    GameManager.Instance.ModifyMemory(source.ownerId == 0 ? -effect.value : effect.value);
                    Debug.Log($"[Effect] {source.cardName} loses {effect.value} memory.");
                    break;
                }

            case EffectType.ExtraSecurityAttack:
                {
                    //Placeholder
                    Debug.Log($"[Effect] {source.cardName} gains Security Attack +{effect.value}");
                    break;
                }

            default:
                {
                    Debug.LogWarning($"[Effect] Unknown effect type: {effect.type}");
                    break;
                }
        }
    }
}
