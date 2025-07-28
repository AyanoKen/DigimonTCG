using System;
using System.Linq;
using System.Collections.Generic;


/* 
    Effects Dictionary

    Effect Triggers:

    OnPlay: Triggers when a card is played into the battle area //Not Implemented
    WhenAttacking: Triggers in Card.AttackSecurity() when attacking
    WhenDigivolving: Triggers when digivolving in GameManager.TryDigivolve()
    YourTurn: Triggers at the start of the player's turn in GameManager.StartTurn()
    OpponentTurn: Triggers at the start of Opponent's turn //Not Implemented
    WhenBlocked: Triggers in GameManager.ResolveSecurityAttack() when blocked


    Effect Types:

    ModifyDP: Gain {value} DP
    ModifyAllyDP: Random ally gains {value} DP
    ModifyPartyDP: All digimon in battle area gain {value} DP
    ModifyDP_ChildCount: While this card has more than {conditionValue} digivolution cards, gain {value} dp
    GainMemory: Gain {value} memory
    LoseMemory: Lose {value} memory
    ExtraSecurityAttack: Gain +{value} security attack
    IncrementSecurityBasedOnChildren: For every {conditionValue} digivolution cards, gain {value} security attack 
    DeleteTargetOpponent: Remove a digimon from opponent's battle area 
    DeleteOpponentDPBelowThreshold: Remove all digimon in opponent's battle area which are below {value} DP
    BuffSecurityDP: 
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
    WhenBlocked,
    Security
}

public enum EffectType
{
    None,
    ModifyDP,
    ModifyAllyDP,
    ModifyPartyDP, 
    ModifyDP_ChildCount, 
    GainMemory,
    LoseMemory,
    ExtraSecurityAttack,
    IncrementSecurityBasedOnChildren, 
    Blocker,
    DeleteTargetOpponent,
    DeleteOpponentDPBelowThreshold,
    BuffSecurityDP, 


    // Security Effects
    PlayCardWithoutMemory,
    ExtraSecurityAttackPartyNextTurn,
    BuffSecurityNextTurn,
    ActivateMainEffect
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
