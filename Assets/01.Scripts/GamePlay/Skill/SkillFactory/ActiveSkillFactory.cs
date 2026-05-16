using Skill;
using System;
using UnityEngine;

public class ActiveSkillFactory
{
    private IServiceLocator _skillContext;
    private ISkillDataRepository _skillDataReader;

    public void Init(IServiceLocator skillContext, ISkillDataRepository skillDataReader)
    {
        _skillContext = skillContext;
        _skillDataReader = skillDataReader;
    }

    public IActiveSkill CreateSkill(int uid, IServiceLocator ownerContext)
    {
        ActiveSkillData data = _skillDataReader.GetActiveSkillData(uid);

        ISkillTriggerStrategy trigger = GetTrigger(data.TriggerType);
        trigger.Init(data.TriggerValue);

        Type type = Type.GetType(data.SkillType);

        if (type != null)
        {
            object[] args = new object[] { data, _skillContext, ownerContext, trigger };

            return (IActiveSkill)Activator.CreateInstance(type, args);
        }
        else
        {
            return null;
        }
    }
    public ISkillTriggerStrategy GetTrigger(ESkillTriggerType triggerType)
    {
        switch (triggerType)
        {
            case ESkillTriggerType.None:
                return new NoneTrigger();

            case ESkillTriggerType.HitCount:
                return new HitCountTrigger();

            case ESkillTriggerType.Mana:
                return new ManaTrigger();
        }

        return null;
    }
}
