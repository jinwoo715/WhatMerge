using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Active Skill", menuName = "Skill/ActiveSkill", order = 0)]
    public class ActiveSkillData : SkillBaseData
    {
        [Header("모션")]
        public SkillAnimationData AnimationData;

        [Header("방식")]
        public ExecutionData Execution;

        [Header("탐색")]
        public TargetData Target;

        [Header("트리거")]
        public TriggerData Trigger;
    }

    [System.Serializable]
    public class SkillAnimationData
    {
        public string MotionName;
        public string MotionReadyName => MotionName + "_Ready";
        public float ReadyMotionTime = 0.2f;
        public float ExecutionMotionTime = 0.2f;
    }
}