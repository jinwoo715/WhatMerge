using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ExtraEffectData", menuName = "Skill/SkillEnhancer/ExtraEffectData", order = 0)]
    public class ExtraEffectData : SkillBaseData
    {
        public SkillBaseData TargetSkill;

        [Header("추가 효과")]
        public EffectEntry Effect;
    }

    public class ExtraEffect : ISkillEnhancer
    {
        public ExtraEffectData EffectEntry { get; }
        public ExtraEffect(ExtraEffectData effectEntry)
        {
            EffectEntry = effectEntry;
        }

        public void ApplySkill(ISkillModifier skill)
        {
            skill.AddEffect(EffectEntry.Effect);
        }
    }
}