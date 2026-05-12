using Heros;
using Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISkillCreater
{
    List<ISkill> CreateActiveSkill(HeroSkillBundle skillBundle, ISkillContext ownerContext);
}
public interface ISkillDataRepository
{
    ActiveSkillData GetActiveSkillData(int uid);
}
public class HeroSkillFactory : ISkillCreater
{
    private ISkillContext _skillContext;
    private ISkillDataRepository _skillRepository;
    public void Init(ISkillContext skillContext, ISkillDataRepository skillRepository)
    {
        _skillContext = skillContext;
        _skillRepository = skillRepository;
    }

    public bool TryCreateActiveSkill(int uid, ISkillContext ownerContext, out ISkill skill)
    {
        if(uid == 0)
        {
            skill = null;
            return false;
        }

        ActiveSkillData data = _skillRepository.GetActiveSkillData(uid);

        ITrigger trigger = GetTrigger(data.TriggerType);
        trigger.Init(data.TriggerValue);

        Type type = Type.GetType(data.SkillType);

        if (type != null)
        {
            object[] args = new object[] { data, _skillContext, ownerContext, trigger };

            skill = (ISkill)Activator.CreateInstance(type, args);
            return true;
        }
        else
        {
            skill = null;
            return false;
        }
    }

    public ITrigger GetTrigger(EExcuteTriggerType triggerType)
    {
        switch (triggerType)
        {
            case EExcuteTriggerType.None:
                return new AlwaysTrigger();

            case EExcuteTriggerType.HitCount:
                return new HitCountTrigger();

            case EExcuteTriggerType.Mana:
                return new ManaTrigger();
        }

        return null;
    }

    public List<ISkill> CreateActiveSkill(HeroSkillBundle skillBundle, ISkillContext ownerContext)
    {
        List<ISkill> skills = new List<ISkill>();

        if (TryCreateActiveSkill(skillBundle.BaseSkill, ownerContext, out var skill1))
            skills.Add(skill1);

        if (TryCreateActiveSkill(skillBundle.FirstSkill, ownerContext, out var skill2))
            skills.Add(skill2);

        if (TryCreateActiveSkill(skillBundle.SecondSkill, ownerContext, out var skill3))
            skills.Add(skill3);

        if (TryCreateActiveSkill(skillBundle.SpecialSkill, ownerContext, out var skill4))
            skills.Add(skill4);

        return skills;
    }
}

//===========================================

public class ProjectileData
{
    public int ProjectileUID;
    public string SpriteName;
    public EProjectileMoveType MoveType;
    public float Speed;
    public float LifeTime;
    public bool LevelSwap;
    public EProjectileAttackType TargetType;
    public EProjectileTrigger DestoryType;
}

public enum EProjectileAttackType
{
    Single,
    Multiple,
    Summon
}


public enum EEffectType
{
    Summon,
    CC
}

public enum EProjectileMoveType
{
    Line,
    Homing,
    Parabola
}

