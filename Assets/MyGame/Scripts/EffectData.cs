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

public enum ConditionType
{
    None,
    CardCount
}

[System.Serializable]
public class ConditionData
{
    public ConditionType conditionType;
    public int conditionValue;

    public ConditionData() { }

    public ConditionData(ConditionType conditionType, int conditionValue)
    {
        this.conditionType = conditionType;
        this.conditionValue = conditionValue;
    }
}

[System.Serializable]
public class EffectData
{
    public EffectTrigger trigger;
    public EffectType type;
    public int value;
    public bool targetSelf;
    public List<ConditionData> conditions = new List<ConditionData>();

    public EffectData() { }

    public EffectData(EffectTrigger trigger, EffectType type, int value, bool targetSelf = true, List<ConditionData> conditions = null)
    {
        this.trigger = trigger;
        this.type = type;
        this.value = value;
        this.targetSelf = targetSelf;
        this.conditions = conditions ?? new List<ConditionData>();
    }
}
