using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "NearHeroFromEnemyTarget", menuName = "Enemy Skill/Target/Near Hero", order = 3)]
    public sealed class NearHeroFromEnemyTargetData : EnemySkillTargetData
    {
        [Min(0f)]
        public float Radius = 1f;

        public override EnemySkillTargetCategory Category => EnemySkillTargetCategory.Hero;
    }
}
