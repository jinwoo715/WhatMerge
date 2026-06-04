using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
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
            skill.ModifyParam(Data.TargetEffect.EffectUID, Data.AddValue);
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
            skill.ModifyChance(Data.TargetEffect.EffectUID, Data.AddChance);
        }
    }

    public interface ISkillEnhancer
    {
        void ApplySkill(ISkillModifier skill);
    }
}
