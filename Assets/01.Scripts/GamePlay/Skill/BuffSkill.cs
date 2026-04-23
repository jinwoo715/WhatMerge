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

public class Data
{
    public int UID;
}

public class BuffData : Data
{
    public EHeroStatType StatType;
    public float BuffValue;
    public float BuffTime;
}

public class BuffDataBundle : Data
{
    public int FirstBuff;
    public int SecondBuff;
    public int ThirdBuff;
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


public abstract class BuffSkill : ActiveSkillBase
{
    public BuffSkill(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }
}

public class AttachBuff : BuffSkill
{
    public AttachBuff(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

    private IBuffRegister _buffRegister;

    public override void BindService()
    {
        BindSkillHelpService(ref _buffRegister);
        Debug.Log(_buffRegister);
    }

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.MotionDelay);

        var heros = CreatureFinder.TryFindNearHeors(_owner.Position, 1);

        Debug.Log($"Buff Target Count : {heros.Count}");

        foreach (var hero in heros)
        {
            _buffRegister.RegisterBuff(Mathf.RoundToInt(_data.P3), hero);
        }

        SetExcuteMotion();

        yield return new WaitForSeconds(_data.ResetDelay);
    }

    public override bool HasValidTarget()
    {
        return true;
    }
}
