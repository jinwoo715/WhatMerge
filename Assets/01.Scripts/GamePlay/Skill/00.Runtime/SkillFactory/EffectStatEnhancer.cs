using Skill.Data;

namespace Skill
{
    public class EffectStatEnhancer : ISkillEnhancer
    {
        public EffectStatEnhanceData Data { get; }

        public EffectStatEnhancer(EffectStatEnhanceData data)
        {
            Data = data;
        }

        public void ApplySkill(ISkillModifier skill)
        {
            if (Data.TargetEffect != null) skill.ModifyParam(Data.TargetEffect, Data.AddValue);
        }
    }

    public class EffectChanceEnhancer : ISkillEnhancer
    {
        public EffectChanceEnhanceData Data { get; }

        public EffectChanceEnhancer(EffectChanceEnhanceData data)
        {
            Data = data;
        }

        public void ApplySkill(ISkillModifier skill)
        {
            if (Data.TargetEffect != null) skill.ModifyChance(Data.TargetEffect, Data.AddChance);
        }
    }

    public interface ISkillEnhancer
    {
        void ApplySkill(ISkillModifier skill);
    }
}
