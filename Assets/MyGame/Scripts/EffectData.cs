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
    MainPhase
}

public enum EffectType
{
    None,
    ModifyDP,
    GainMemory,
    LoseMemory,
    ExtraSecurityAttack,
    Blocker
}

[System.Serializable]
public class EffectData
{
    public EffectTrigger trigger;
    public EffectType type;
    public int value;

    public EffectData() { }

    public EffectData(EffectTrigger trigger, EffectType type, int value)
    {
        this.trigger = trigger;
        this.type = type;
        this.value = value;
    }
}
