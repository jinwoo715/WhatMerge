using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "EffectChanceEnhaner", menuName = "Skill/SkillEnhancer/EffectChanceEnhaner", order = 0)]
    public class EffectChanceEnhanceData : Enhancer
    {
        [Header("È®·ü »ó½Â ¼öÄ¡")]
        [Range(0, 1)]
        public float AddChance;
    }
}
