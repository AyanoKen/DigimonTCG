using System;
using System.Linq;
using System.Collections.Generic;


/* 
Effects Dictionary

Effect Trigers:

OnPlay: Triggers when a card is played into the battle area
WhenAttacking: Triggers while resolving Security attack
WhenDigivolving: Triggers when digivolving
YourTurn: Triggers at the start of the player's turn
OpponentTurn: Triggers at the start of Opponent's turn










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
