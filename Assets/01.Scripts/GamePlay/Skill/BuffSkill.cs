using Combat;
using Skill;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 버프
// 공격력, 속도, 범위, 마나 충전속도, 크리티컬, 크리배수
// 시간
// 상승 수치

//n개가 있을 수 있다.

//실행한다.
//버프 진행중 위치를 옮기면 어떻게 되는가?


public class BuffData
{
    public int BuffUID;
    public EHeroStatType StatType;
    public float BuffValue;
}

public enum EHeroStatType
{
    AttackDamage,
    AttackSpeed,
    AttackRange,
    ManaChargeSpeed,
    CriticalChance,
    CriticalMultiplier,
    FlatPentration,
    PercentPenetration,
}


public class BuffSkill : ActiveSkillBase
{
    public BuffSkill(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

    public override void BindService()
    {
        
    }

    public override IEnumerator Excute()
    {
        

        yield break;
    }

    public override bool HasValidTarget()
    {
        return false;
    }
}
