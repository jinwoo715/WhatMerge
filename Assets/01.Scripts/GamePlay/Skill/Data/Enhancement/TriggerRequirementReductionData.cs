using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(
        fileName = "TriggerRequirementReduction",
        menuName = "Skill/SkillEnhancer/TriggerRequirementReduction",
        order = 0)]
    public sealed class TriggerRequirementReductionData : SkillBaseData
    {
        [Header("대상 Skill")]
        public ActiveSkillData TargetSkill;

        [Header("요구량 감소 비율")]
        [Tooltip("0.1은 원본 요구량의 10% 감소를 의미합니다.")]
        [Range(0f, 1f)]
        public float ReductionRatio;
    }
}
