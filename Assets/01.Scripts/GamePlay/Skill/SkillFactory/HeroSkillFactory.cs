using Entity;
using Heros;
using Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISkillFactory
{
    List<IActiveSkill> CreateActiveSkill(HeroSkillBundle skillBundle, IServiceLocator ownerContext);
    List<ISkill> CreateSkills(List<string> skillNames, IServiceLocator ownerContext, Hero hero);
}
public interface ISkillDataRepository
{
    ActiveSkillData GetActiveSkillData(int uid);
    BuffData GetBuffData(int uid);
    DeBuffData GetDeBuffData(int uid);
    ExtraEffectData GetExtraEffectData(int uid);
    SkillStatModifyData GetSkillModifierData(int uid);
    HeroUpgradeSkillData GetHeroUpgradeSkillData(int uid);
}
public class HeroSkillFactory : ISkillFactory
{
    private IServiceLocator _skillContext;
    private ISkillDataRepository _skillRepository;

    private IFieldHeroService fieldHeroService;
    private ActiveSkillFactory _activeSkillFactory;

    public void Init(IServiceLocator skillContext, ISkillDataRepository skillRepository)
    {
        _skillContext = skillContext;
        _skillRepository = skillRepository;
    }

    public List<IActiveSkill> CreateActiveSkill(HeroSkillBundle skillBundle, IServiceLocator ownerContext)
    {
        List<IActiveSkill> skills = new List<IActiveSkill>();

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

    public List<ISkill> CreateSkill(List<string> skills, IServiceLocator ownerContext, Hero hero)
    {
        List<ISkill> skiils = new List<ISkill>();

        foreach (var item in skills)
        {
            string[] str = item.Split("-");
            int skillUid = int.Parse(str[1]);

            switch (str[0])
            {
                case "A":
                    skiils.Add(_activeSkillFactory.CreateSkill(skillUid, ownerContext));
                    break;
                case "B":
                    var data = _skillRepository.GetBuffData(skillUid);
                    var buff = CreateBuff(data);
                    buff.Init(fieldHeroService, hero, data);

                    skiils.Add(buff);

                    break;
                case "D":
                    break;
            }
        }

        return skiils;
    }

    public BuffBase CreateBuff(BuffData buffData)
    {
        if (buffData.TargetType == EBuffTargetType.Self)
        {
            return new SelfBuff();
        }
        else if (buffData.TargetType == EBuffTargetType.NearHeros)
        {
            return new NearBuff();
        }
        else
            return new AllBuff();
    }

    public bool TryCreateActiveSkill(int uid, IServiceLocator ownerContext, out IActiveSkill skill)
    {
        if (uid == 0)
        {
            skill = null;
            return false;
        }

        var createdSkill = _activeSkillFactory.CreateSkill(uid, ownerContext);

        if(createdSkill == null)
        {
            skill = null;
            return false;
        }
        else
        {
            skill = createdSkill;
            return true;
        }
    }

    public List<ISkill> CreateSkills(List<string> skillNames, IServiceLocator ownerContext, Hero owner)
    {
        return CreateSkill(skillNames, ownerContext, owner);
    }
}

//===========================================

public enum EProjectileAttackType
{
    Single,
    Multiple,
    Summon
}

public enum EProjectileMoveType
{
    Line,
    Homing,
    Parabola
}

