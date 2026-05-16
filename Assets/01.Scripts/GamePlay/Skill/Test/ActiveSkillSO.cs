using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Active Skill", menuName = "Skill/ActiveBase", order = 0)]
    public class ActiveSkillSO : ScriptableObject
    {
        [Header("Info")]
        public int UID;
        public string Name;
        public string Description;

        [Header("Animation")]
        public string MotionName;
        public float ReadyMotionMotion = 0.2f;
        public float ExecutionMotionTime = 0.2f;

        [Header("방식")]
        public ExecutionSystem ActiveAction;

        [Header("공격시 효과")]
        public SkillVisualSystem VFX;

        [Header("탐색")]
        public TargetSystem Target;

        [Header("트리거")]
        public TriggerSystem Trigger;

        [Header("이펙트")]
        public List<EffectEntry> Effects;
    }

    public class ExecutionSystem : ScriptableObject
    {
        public int Value;
    }

    [System.Serializable]
    public class EffectEntry
    {
        public SkillEffect Effect;

        [Range(0, 1)]
        public float Chance = 1f;
    }
}