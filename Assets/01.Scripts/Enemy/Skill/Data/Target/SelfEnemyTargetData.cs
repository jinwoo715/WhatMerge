using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "SelfEnemyTarget", menuName = "Enemy Skill/Target/Self Enemy", order = 0)]
    public sealed class SelfEnemyTargetData : EnemySkillTargetData
    {
        public override EnemySkillTargetCategory Category => EnemySkillTargetCategory.Enemy;
    }
}
