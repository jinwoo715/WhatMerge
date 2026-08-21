using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(
        fileName = "ActivationChanceEnhancer",
        menuName = "Skill/SkillEnhancer/ActivationChanceEnhancer",
        order = 0)]
    public sealed class ActivationChanceEnhanceData : SkillBaseData
    {
        [Header("대상 Skill")]
        public ActiveSkillData TargetSkill;

        [Header("발동 확률 증가량")]
        [Tooltip("0.1은 발동 확률 10%p 증가를 의미합니다.")]
        [Range(0f, 1f)]
        public float AddChance;
    }
}
