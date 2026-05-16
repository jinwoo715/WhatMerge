using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ExtraEffect", menuName = "Skill/SkillEnhancer/ExtraEffect", order = 0)]
    public class ExtraEffect : SkillEnhancer
    {
        public int TargetSkillUID;
        public EffectEntry Effect;
    }
}