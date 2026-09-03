using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "AllHeroFromEnemyTarget", menuName = "Enemy Skill/Target/All Heroes", order = 4)]
    public sealed class AllHeroFromEnemyTargetData : EnemySkillTargetData
    {
        public override EnemySkillTargetCategory Category => EnemySkillTargetCategory.Hero;
    }
}
