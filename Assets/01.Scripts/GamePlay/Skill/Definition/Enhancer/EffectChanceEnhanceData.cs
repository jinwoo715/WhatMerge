using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "EffectChanceEnhaner", menuName = "Skill/SkillEnhancer/EffectChanceEnhaner", order = 0)]
    public class EffectChanceEnhanceData : Enhancer
    {
        [Header("강화 수치")]
        [Range(0, 1)]
        public float AddChance;
    }
}
