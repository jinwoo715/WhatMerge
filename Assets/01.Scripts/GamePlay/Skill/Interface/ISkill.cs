using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SkillTriggerContext
{
    public int HitCount;
    public float Mana;

    public SkillTriggerContext(int hitCount, float mana)
    {
        HitCount = hitCount;
        Mana = mana;
    }
}

public enum ESkillSlot 
{
    Basic,
    First,
    Second,
    Special
}

public interface ISkill
{
    IEnumerator Excute();
    bool IsUseable(SkillTriggerContext context);
    void PayCost(ISkillResourceModifier skillResourceModifier);
}
