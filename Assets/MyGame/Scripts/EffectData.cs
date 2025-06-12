using System;
using System.Linq; 
using System.Collections.Generic;

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
    ModifyDP_ChildCount, //TODO
    GainMemory,
    LoseMemory,
    ExtraSecurityAttack,
    IncrementSecurityBasedOnChildren, //TODO
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
