using UnityEngine;
using UnityEngine.Serialization;

namespace Skill.Data
{
    public enum TriggerRequirementReductionType
    {
        Ratio,
        Fixed
    }

    [CreateAssetMenu(
        fileName = "TriggerRequirementReduction",
        menuName = "Skill/SkillEnhancer/TriggerRequirementReduction",
        order = 0)]
    public sealed class TriggerRequirementReductionData : SkillBaseData
    {
        [Header("대상 Skill")]
        public ActiveSkillData TargetSkill;

        [Header("요구량 감소")]
        public TriggerRequirementReductionType ReductionType;

        [FormerlySerializedAs("ReductionRatio")]
        [Tooltip("Ratio는 0.1을 10%로, Fixed는 입력값을 고정 감소량으로 사용합니다.")]
        [Min(0f)]
        public float ReductionValue;
    }
}
