using Combat;
using Skill.Data;
using System.Collections;

namespace Skill
{
    public interface ISkill
    {
        int UID { get; }
    }
    public interface ISkillModifier
    {
        public void ModifyParam(EffectBase targetEffect, float value);
        public void ModifyChance(EffectBase targetEffect, float value);
        public void AddEffect(EffectBase effect);
    }
    public interface IActiveSkill : ISkill, ISkillModifier
    {
        public ITrigger Trigger { get; }
        public ITarget Target { get; }
        public IExecute Execution { get; }
        bool IsUsable(SkillTriggerContext context);
        IEnumerator Execute();
    }
    public interface IPassiveSkill : ISkill
    {
        void Apply();
    }
    public interface ISkillResourceModifier
    {
        void ConsumeHitCount(int count);
        void ConsumeMana(float amount);
        void AddHitCount(int count);
        void AddMana(float amount);
        void IncreaseManaAmoutRaio(float ratio);
    }
    public interface ITrigger
    {
        bool IsMeetTrigger(SkillTriggerContext context);
        void UseTriggerResource(ISkillResourceModifier resourceModifier);
    }
}