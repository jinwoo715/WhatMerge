using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemyHitCountTrigger", menuName = "Enemy Skill/Trigger/Hit Count", order = 1)]
    public sealed class EnemyHitCountTriggerData : EnemySkillTriggerData
    {
        [Min(1)]
        public int RequiredHitCount = 1;
    }
}
