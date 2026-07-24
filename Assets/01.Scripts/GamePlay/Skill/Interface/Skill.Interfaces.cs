using Skill.Data;
using System.Collections;

namespace Skill
{
    public interface ISkill
    {
        int SkillUID { get; }
    }

    public interface IActiveSkill : ISkill
    {
        public ITrigger Trigger { get; }
        public IFinder Target { get; }
        public IExecute Execution { get; }
        bool IsUsable(SkillTriggerContext context);
        IEnumerator Execute();
        void Dispose();
    }
    public interface IPassiveSkill : ISkill
    {
        void Apply();
        void Release();
    }
    public interface ISkillResourceModifier
    {
        void ConsumeHitCount(int count);
        void ConsumeMana(float amout);
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