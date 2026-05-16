using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "EffectStatAdder", menuName = "Skill/SkillEnhancer/EffectStatAdder", order = 0)]
    public class EffectStatAdder : SkillEnhancer
    {
        public int EffectUID;
        public int AddValue;
    }
}
