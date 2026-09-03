using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "TriggeredEnemyTarget", menuName = "Enemy Skill/Target/Triggered Enemy", order = 5)]
    public sealed class TriggeredEnemyTargetData : EnemySkillTargetData
    {
        public override EnemySkillTargetCategory Category => EnemySkillTargetCategory.Enemy;
    }
}
