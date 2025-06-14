using System;
using System.Linq;
using System.Collections.Generic;


/* 
Effects Dictionary

Effect Triggers:

OnPlay: Triggers when a card is played into the battle area
WhenAttacking: Triggers in Card.AttackSecurity() when attacking
WhenDigivolving: Triggers when digivolving in GameManager.TryDigivolve()
YourTurn: Triggers at the start of the player's turn
OpponentTurn: Triggers at the start of Opponent's turn
WhenBlocked: Triggers in GameManager.ResolveSecurityAttack() when blocked


Effect Types:

ModifyDP: Gain {value} DP
ModifyAllyDP: Random ally gains {value} DP
ModifyPartyDP: //TODO
ModifyDP_ChildCount: While this card has more than {conditionValue} digivolution cards, gain {value} dp
GainMemory: Gain {value} memory
LoseMemory: Lose {value} memory
ExtraSecurityAttack: Gain +{value} security attack
IncrementSecurityBasedOnChildren: For every {conditionValue} digivolution cards, gain {value} security attack 
DeleteTargetOpponent: //TODO
DeleteOpponentDPBelowThreshold: //TODO
BuffSecurityDP: //TODO
*/

public enum EffectTrigger 
{
    None,
    OnPlay,
    WhenAttacking,
    WhenDigivolving,
    YourTurn,
    OpponentTurn,
    MainPhase,
    WhenBlocked
}

public enum EffectType
{
    None,
    ModifyDP,
    ModifyAllyDP,
    ModifyPartyDP, //TODO
    ModifyDP_ChildCount, // While this card has more than {conditionValue} digivolution cards, gain {value} dp
    GainMemory,
    LoseMemory,
    ExtraSecurityAttack,
    IncrementSecurityBasedOnChildren, // 
    Blocker,
    DeleteTargetOpponent, //TODO
    DeleteOpponentDPBelowThreshold, //TODO
    BuffSecurityDP //TODO
}

[System.Serializable]
public class EffectData
{
    public EffectTrigger trigger;
    public EffectType type;
    public int value;
    public int conditionValue;

    public EffectData() { }

    public EffectData(EffectTrigger trigger, EffectType type, int value, int conditionValue = 0)
    {
        this.trigger = trigger;
        this.type = type;
        this.value = value;
        this.conditionValue = conditionValue;
    }
}
