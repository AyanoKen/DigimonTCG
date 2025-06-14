using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

            case EffectType.ModifyAllyDP:
                {
                    var candidates = FindObjectsOfType<Card>()
                        .Where(c => c.ownerId == source.ownerId
                                && c.currentZone == Card.Zone.BattleArea
                                && c != source)
                        .ToList();

                    if (candidates.Count > 0)
                    {
                        Card target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                        target.dpBuff += effect.value;
                        Debug.Log($"[Effect] {target.cardName} gains {effect.value} DP (ModifyAllyDP).");
                    }
                    else
                    {
                        Debug.Log("No valid allies found for ModifyAllyDP.");
                    }

                    break;
                }

            case EffectType.ModifyPartyDP:
                {
                    var party = FindObjectsOfType<Card>()
                        .Where(c => c.ownerId == source.ownerId && c.currentZone == Card.Zone.BattleArea)
                        .ToList();

                    foreach (var member in party)
                    {
                        member.dpBuff += effect.value;
                        Debug.Log($"[Effect] {member.cardName} gains {effect.value} DP from Party Buff.");
                    }

                    break;
                }

            case EffectType.ModifyDP_ChildCount:
                {
                    if (source.inheritedStack.Count > effect.conditionValue)
                    {
                        source.dpBuff += effect.value;
                        Debug.Log($"[Effect] {source.cardName} gains {effect.value} DP from inherited stack condition.");
                    }
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
                    source.securityAttackCount += effect.value;
                    Debug.Log($"[Effect] {source.cardName} gains Security Attack +{effect.value} for the turn");
                    break;
                }

            case EffectType.IncrementSecurityBasedOnChildren:
                {
                    int bonus = (source.inheritedStack.Count / effect.conditionValue) * effect.value;
                    source.securityAttackCount += bonus;
                    Debug.Log($"[Effect] {source.cardName} gains +{bonus} security attack based on child stack.");
                    break;
                }

            case EffectType.DeleteTargetOpponent:
                {
                    var opponents = FindObjectsOfType<Card>()
                        .Where(c => c.ownerId != source.ownerId 
                                    && c.currentZone == Card.Zone.BattleArea)
                        .ToList();

                    if (opponents.Count > 0)
                    {
                        Card target = opponents[UnityEngine.Random.Range(0, opponents.Count)];
                        GameManager.Instance.SendToTrash(target);
                        Debug.Log($"[Effect] Deleted {target.cardName} (DeleteTargetOpponent).");
                    }
                    else
                    {
                        Debug.Log("No valid opponent Digimon to delete.");
                    }
                    break;
                }
            

            case EffectType.DeleteOpponentDPBelowThreshold:
                {
                    var targets = FindObjectsOfType<Card>()
                        .Where(c => c.ownerId != source.ownerId
                                    && c.currentZone == Card.Zone.BattleArea
                                    && c.dp <= effect.value)
                        .ToList();

                    for (int i = 0; i < effect.conditionValue; i++)
                    {
                        if (i < targets.Count)
                        {
                            GameManager.Instance.SendToTrash(targets[i]);
                            Debug.Log($"[Effect] Deleted {targets[i].cardName} with DP ≤ {effect.value}.");
                        }
                    }
                    break;
                }
            

            case EffectType.BuffSecurityDP:
                {
                    Debug.Log("[Effect] Buffing security stack DP -- (placeholder)");
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
