using Skill.Data;
using UnityEngine;

namespace Skill
{
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