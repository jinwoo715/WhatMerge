using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemySkill", menuName = "Enemy Skill/Skill", order = 2)]
    public sealed class EnemySkillData : ScriptableObject
    {
        public string Name;

        [TextArea]
        public string Description;

        public EnemySkillTriggerData Trigger;
        public EnemySkillExecutionPolicy ExecutionPolicy = new EnemySkillExecutionPolicy();
        public List<EnemySkillActionData> Actions = new List<EnemySkillActionData>();
    }

    [Serializable]
    public sealed class EnemySkillExecutionPolicy
    {
        [Min(0)]
        public int Priority;

        [Min(0f)]
        public float Cooldown;

        [Tooltip("Zero allows unlimited activations.")]
        [Min(0)]
        public int MaxActivationCount = 1;
    }

    [Serializable]
    public sealed class EnemySkillActionData
    {
        public EnemySkillTargetData Target;
        public List<EnemySkillEffectData> Effects = new List<EnemySkillEffectData>();
    }
}
