using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "EffectStatAdder", menuName = "Skill/SkillEnhancer/EffectStatAdder", order = 0)]
    public class EffectStatAdderData : SkillEnhancer
    {
        [Header("강화 효과")]
        public EffectBase TargetEffect;

        [Header("강화 수치")]
        public int AddValue;
    }

    public class EffectChanceAdderData : SkillEnhancer
    {
        [Header("강화 효과")]
        public EffectBase TargetEffect;

        [Header("강화 수치")]
        [Range(0,1)]
        public float AddChance;
    }
}
