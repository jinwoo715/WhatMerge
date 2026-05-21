using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Skill.Data
{
    [CreateAssetMenu(fileName = "SkillEntry", menuName = "Skill/SkillEntry", order =0)]
    public class EffectEntry : ScriptableObject
    {
        public EffectBase Effect;

        [Range(0, 1)]
        public float Chance = 1f;
    }
}