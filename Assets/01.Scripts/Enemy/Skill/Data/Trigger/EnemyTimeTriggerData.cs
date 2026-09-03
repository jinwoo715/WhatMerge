using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemyTimeTrigger", menuName = "Enemy Skill/Trigger/Time", order = 0)]
    public sealed class EnemyTimeTriggerData : EnemySkillTriggerData
    {
        [Min(0f)]
        public float InitialDelay;

        [Min(0f)]
        public float Interval;
    }
}
