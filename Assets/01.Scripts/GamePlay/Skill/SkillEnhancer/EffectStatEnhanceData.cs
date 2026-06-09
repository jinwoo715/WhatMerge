using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "EffectStatEnhancer", menuName = "Skill/SkillEnhancer/EffectStatEnhancer", order = 0)]
    public class EffectStatEnhanceData : SkillEnhancer
    {
        [Header("강화 수치")]
        public float AddValue;
    }
}
