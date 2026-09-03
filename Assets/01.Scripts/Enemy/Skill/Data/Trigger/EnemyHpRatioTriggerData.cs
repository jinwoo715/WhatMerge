using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemyHpRatioTrigger", menuName = "Enemy Skill/Trigger/HP Ratio", order = 2)]
    public sealed class EnemyHpRatioTriggerData : EnemySkillTriggerData
    {
        [Range(0f, 1f)]
        public float ThresholdRatio = 0.5f;
    }
}
