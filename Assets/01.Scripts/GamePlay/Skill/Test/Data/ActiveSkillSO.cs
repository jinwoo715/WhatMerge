using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Active Skill", menuName = "Skill/ActiveSkill", order = 0)]
    public class ActiveSkillSO : SkillBase
    {
        [Header("Info")]
        public string Name;
        public string Description;

        [Header("모션")]
        public SkillAnimationData AnimationData;

        [Header("방식")]
        public ExecutionSystem Execution;

        [Header("탐색")]
        public TargetSystem Target;

        [Header("트리거")]
        public TriggerSystem Trigger;
    }

    [System.Serializable]
    public class SkillAnimationData
    {
        public string MotionName;
        public string MotionReadyName => MotionName + "_Ready";
        public float ReadyMotionTime = 0.2f;
        public float ExecutionMotionTime = 0.2f;
    }

    public class ExecutionSystem : ScriptableObject
    {
        [Header("이펙트")]
        public List<EffectEntry> Effects;

        [Header("공격시 효과")]
        public VisualEffectData VFX;
    }

    [System.Serializable]
    public class EffectEntry
    {
        public SkillEffect Effect;

        [Range(0, 1)]
        public float Chance = 1f;
    }
}