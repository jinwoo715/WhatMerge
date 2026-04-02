using Heros;
using Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISkillCreater
{
    //ISkill CreateActiveSkill(int uid, ISkillContext ownerContext);
    List<ISkill> CreateActiveSkill(HeroSkillBundle skillBundle, ISkillContext ownerContext);
}

public interface ISkillRepository
{
    ActiveSkillData GetActiveSkillData(int uid);
}

public class HeroSkillFactory : ISkillCreater
{
    private ISkillContext _skillContext;
    private ISkillRepository _skillRepository;
    public void Init(ISkillContext skillContext, ISkillRepository skillRepository)
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
