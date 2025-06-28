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

                    BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName} gains {effect.value} DP.",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);

                    Debug.Log($"[Effect] {source.cardName} gains {effect.value} DP.");

                    break;
                }

            case EffectType.ModifyAllyDP:
                {
                    var candidates = FindObjectsOfType<Card>()
                        .Where(c => c.ownerId == source.ownerId
                                && c.currentZone.Value == Card.Zone.BattleArea
                                && c != source && !c.isDigivolved)
                        .ToList();

                    if (candidates.Count > 0)
                    {
                        Card target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                        target.dpBuff += effect.value;

                        BattleLogManager.Instance.AddLog(
                            $"[Effect] {target.cardName} gains {effect.value} DP (ModifyAllyDP).",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);
                        
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
                        .Where(c => c.ownerId == source.ownerId && c.currentZone.Value == Card.Zone.BattleArea && !c.isDigivolved)
                        .ToList();

                    foreach (var member in party)
                    {
                        member.dpBuff += effect.value;

                        BattleLogManager.Instance.AddLog(
                            $"[Effect] {member.cardName} gains {effect.value} DP from Party Buff.",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);

                        Debug.Log($"[Effect] {member.cardName} gains {effect.value} DP from Party Buff.");
                    }

                    break;
                }

            case EffectType.ModifyDP_ChildCount:
                {
                    if (source.inheritedStack.Count > effect.conditionValue)
                    {
                        source.dpBuff += effect.value;

                        BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName} gains {effect.value} DP from inherited stack condition.",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);

                        Debug.Log($"[Effect] {source.cardName} gains {effect.value} DP from inherited stack condition.");
                    }
                    break;
                }

            case EffectType.GainMemory:
                {
                    GameManager.Instance.ModifyMemoryServerRpc(source.ownerId == 0 ? -effect.value : effect.value);

                    BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName}, player gains {effect.value} memory.",
                            BattleLogManager.LogType.System,
                            source.ownerId);

                    Debug.Log($"[Effect] {source.cardName} gains {effect.value} memory.");
                    break;
                }

            case EffectType.LoseMemory:
                {
                    GameManager.Instance.ModifyMemoryServerRpc(source.ownerId == 0 ? effect.value : -effect.value);

                    BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName}, player loses {effect.value} memory.",
                            BattleLogManager.LogType.System,
                            source.ownerId);

                    Debug.Log($"[Effect] {source.cardName} loses {effect.value} memory.");
                    break;
                }

            case EffectType.ExtraSecurityAttack:
                {
                    source.securityAttackCount += effect.value;

                    BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName} gains Security Attack +{effect.value} for the turn",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);

                    Debug.Log($"[Effect] {source.cardName} gains Security Attack +{effect.value} for the turn");
                    break;
                }

            case EffectType.IncrementSecurityBasedOnChildren:
                {
                    int bonus = (source.inheritedStack.Count / effect.conditionValue) * effect.value;
                    source.securityAttackCount += bonus;

                    BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName} gains +{bonus} security attack based on child stack.",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);

                    Debug.Log($"[Effect] {source.cardName} gains +{bonus} security attack based on child stack.");
                    break;
                }

            case EffectType.DeleteTargetOpponent:
                {
                    var opponents = FindObjectsOfType<Card>()
                        .Where(c => c.ownerId != source.ownerId 
                                    && c.currentZone.Value == Card.Zone.BattleArea && !c.isDigivolved)
                        .ToList();

                    if (opponents.Count > 0)
                    {
                        Card target = opponents[UnityEngine.Random.Range(0, opponents.Count)];
                        GameManager.Instance.DestroyDigimonStack(target);

                        BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName}, Deleted {target.cardName}.",
                            BattleLogManager.LogType.Destroy,
                            source.ownerId);

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
                                    && c.currentZone.Value == Card.Zone.BattleArea
                                    && c.dp <= effect.value && !c.isDigivolved)
                        .ToList();

                    for (int i = 0; i < effect.conditionValue; i++)
                    {
                        if (i < targets.Count)
                        {
                            GameManager.Instance.DestroyDigimonStack(targets[i]);

                            BattleLogManager.Instance.AddLog(
                                $"[Effect] {source.cardName}, Deleted {targets[i].cardName} with DP ≤ {effect.value}.",
                                BattleLogManager.LogType.Destroy,
                                source.ownerId);

                            Debug.Log($"[Effect] Deleted {targets[i].cardName} with DP ≤ {effect.value}.");
                        }
                    }
                    break;
                }

            case EffectType.PlayCardWithoutMemory:
                {
                    Debug.Log("[Security Effect] Playing card directly onto field without paying cost.");

                    BattleLogManager.Instance.AddLog(
                            $"[Security Effect] {source.cardName}, Playing card directly onto field without paying cost.",
                            BattleLogManager.LogType.System,
                            source.ownerId);

                    Transform targetZone = GameManager.Instance.opponentTamerZone;

                    if (source.ownerId == 0)
                    {
                        targetZone = GameManager.Instance.playerTamerZone;
                    }

                    if (source.cardType == "Tamer")
                    {
                        source.transform.SetParent(targetZone);
                        source.currentZone.Value = Card.Zone.TamerArea;

                        CanvasGroup cg = source.GetComponent<CanvasGroup>();
                        if (cg != null)
                        {
                            cg.blocksRaycasts = true;
                        }
                    }

                    break;
                }

            case EffectType.ExtraSecurityAttackPartyNextTurn:
                {
                    Debug.Log("[Security Effect] All Digimon get +Security Attack next turn.");

                    BattleLogManager.Instance.AddLog(
                            $"[Security Effect] {source.cardName}, All Digimon get +1 Security Attack next turn",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);

                    var party = FindObjectsOfType<Card>()
                        .Where(c => c.ownerId == source.ownerId && c.currentZone.Value == Card.Zone.BattleArea && !c.isDigivolved)
                        .ToList();

                    foreach (var member in party)
                    {
                        member.securityAttackCount += effect.value;
                    }
                    break;
                }

            case EffectType.BuffSecurityNextTurn:
                {
                    Debug.Log("[Security Effect] Security Digimon gain +" + effect.value + " DP for next turn.");

                    BattleLogManager.Instance.AddLog(
                            $"[Security Effect] {source.cardName}, Security Digimon gain +" + effect.value + " DP until end of turn.",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);

                    if (source.ownerId == 0)
                    {
                        GameManager.Instance.player1SecurityBuff += effect.value;
                    }
                    else
                    {
                        GameManager.Instance.player2SecurityBuff += effect.value;
                    }
                    break;
                }

            case EffectType.ActivateMainEffect:
                {
                    Debug.Log("[Security Effect] Activating card's main effect.");

                    BattleLogManager.Instance.AddLog(
                            $"[Security Effect] {source.cardName}, Activating card's main effect",
                            BattleLogManager.LogType.System,
                            source.ownerId);

                    TriggerEffects(EffectTrigger.MainPhase, source);
                    break;
                }
            

            case EffectType.BuffSecurityDP:
                {
                    Debug.Log("[Effect] Buffing security stack DP");

                    BattleLogManager.Instance.AddLog(
                            $"[Effect] {source.cardName}, Buffing security stack DP",
                            BattleLogManager.LogType.Buff,
                            source.ownerId);
                    
                    if (source.ownerId == 0)
                    {
                        GameManager.Instance.player1SecurityBuff += effect.value;
                    }
                    else
                    {
                        GameManager.Instance.player2SecurityBuff += effect.value;
                    }
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
