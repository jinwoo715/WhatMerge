using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemyProximityTrigger", menuName = "Enemy Skill/Trigger/Enemy Proximity", order = 4)]
    public sealed class EnemyProximityTriggerData : EnemySkillTriggerData
    {
        [Min(1)]
        public int TargetEnemyUID;

        [Tooltip("Center-to-center distance in world units.")]
        [Min(0.0001f)]
        public float DetectionDistance = 0.1f;
    }
}
