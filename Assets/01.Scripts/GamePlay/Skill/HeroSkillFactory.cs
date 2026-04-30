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
        Type type = Type.GetType(data.SkillType);

        if (type != null)
        {
            object[] args = new object[] { data, _skillContext, ownerContext };

            skill = (ISkill)Activator.CreateInstance(type, args);
            return true;
        }
        else
        {
            skill = null;
            return false;
        }
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

public class HeroSkillData
{
    public int HeroUID;
    public int SlotIndex;
    public string Name;
    public string Description;
    public string Motion;
    public ESkillType SkillType;
    public int SkillRefID;
    public EExcuteTriggerType TriggerType;
    public int TriggerValue;
    public int DmgRate;
}

public enum ESkillType
{
    Melee,
    Projectile,
    Summon
}

public class SkillDataBase
{
    public int SkillUID;

    public int P1;
    public int P2;

    public string VFX;
    public List<int> EffectIds;
}

public class MeleeSkillData : SkillDataBase
{
    public EMeleeAttackType SkillType;
}

public enum EMeleeAttackType
{
    SingleAttack,
    MultiAttack,
    ConeAttack
}



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

