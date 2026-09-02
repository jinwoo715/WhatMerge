using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(
        fileName = "SequenceCountEnhance",
        menuName = "Skill/SkillEnhancer/SequenceCountEnhance",
        order = 0)]
    public sealed class SequenceCountEnhanceData : SkillBaseData
    {
        [Header("대상 Skill")]
        public ActiveSkillData TargetSkill;

        [Header("추가 타격 횟수")]
        [Min(1)]
        public int AddCount = 1;
    }
}
