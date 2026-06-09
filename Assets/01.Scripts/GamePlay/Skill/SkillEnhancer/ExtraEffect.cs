using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ExtraEffectData", menuName = "Skill/SkillEnhancer/ExtraEffectData", order = 0)]
    public class ExtraEffectData : SkillBaseData
    {
        public ActiveSkillData TargetSkill;

        [Header("추가 효과")]
        public EffectBase Effect;
    }

    public class ExtraEffect : ISkillEnhancer
    {
        public ExtraEffectData Data { get; }
        public ExtraEffect(ExtraEffectData effectEntry)
        {
            Data = effectEntry;
        }

        public void ApplySkill(ISkillModifier skill)
        {
            skill.AddEffect(Data.Effect);
        }
    }
}