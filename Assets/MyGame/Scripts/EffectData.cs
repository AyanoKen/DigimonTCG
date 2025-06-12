using System;

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

[Serializable]
public class EffectData
{
    public EffectTrigger trigger;
    public EffectType type;
    public int value;
    public bool targetSelf;

    public EffectData() { }

    public EffectData(EffectTrigger trigger, EffectType type, int value, bool targetSelf = true)
    {
        this.trigger = trigger;
        this.type = type;
        this.value = value;
        this.targetSelf = targetSelf;
    }
}
