using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ExtraEffectData", menuName = "Skill/SkillEnhancer/ExtraEffectData", order = 0)]
    public class ExtraEffectData : SkillBaseData
    {
        [Header("대상 Skill")]
        public ActiveSkillData TargetSkill;

        [Header("추가할 컨테이너")]
        public ScriptableObject EffectContainer;

        [Header("추가할 효과")]
        public EffectBase Effect;
    }
}