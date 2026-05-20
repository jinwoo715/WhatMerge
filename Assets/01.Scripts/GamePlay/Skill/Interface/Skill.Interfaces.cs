using Combat;
using System.Collections;

namespace Skill
{
    public interface ISkill
    {
        int UID { get; }
        public void ModifyParam(int paramIndex, float value);
    }
    public interface IActiveSkill : ISkill
    {
        IEnumerator Execute();
        void RegisterExtraEffect(ISkillExtraEffecter extraEffecter);
        bool IsUseable(SkillTriggerContext context);
        void PayCost(ISkillResourceModifier skillResourceModifier);
    }
    public interface IPassiveSkill : ISkill
    {
        void Apply();
        void Remove();
    }

    public interface ISkillExtraEffecter
    {
        public int TargetSkillUID { get; }
        void OnBeforeApply(AttackPayload payload);
    }
    public interface ISkillTriggerStrategy
    {
        void Init(float cost);
        bool CanTrigger(SkillTriggerContext context);
        void PayCost(ISkillResourceModifier resourceModifier);
    }
    public interface ISkillResourceModifier
    {
        void ConsumeHitCount(int count);
        void ConsumeMana(float amount);
        void AddHitCount(int count);
        void AddMana(float amount);
        void IncreaseManaAmoutRaio(float ratio);
    }
    

}