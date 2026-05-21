using Combat;
using Skill.Data;
using System.Collections;

namespace Skill
{
    public interface ISkill
    {
        int UID { get; }
        public void ModifyParam(int effectIndex, float value);
        public void ModifyChance(int effectIndex, float value);
        public void AddEffect(EffectEntry effect);
    }

    public interface IPassiveSkill : ISkill
    {
        void Apply();
        void Remove();
    }
    public interface ISkillResourceModifier
    {
        void ConsumeHitCount(int count);
        void ConsumeMana(float amount);
        void AddHitCount(int count);
        void AddMana(float amount);
        void IncreaseManaAmoutRaio(float ratio);
    }
    public interface ISkillDataRepository
    {
        ActiveSkillData GetActiveSkillData(int uid);
        BuffData GetBuffData(int uid);
        DeBuffData GetDeBuffData(int uid);
        ExtraEffectData GetExtraEffectData(int uid);
    }
}