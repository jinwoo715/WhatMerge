using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "NearAllyEnemyTarget", menuName = "Enemy Skill/Target/Near Ally Enemy", order = 1)]
    public sealed class NearAllyEnemyTargetData : EnemySkillTargetData
    {
        [Min(0f)]
        public float Radius = 1f;

        public bool IncludeSelf;
        public List<EnemyType> AllowedTypes = new List<EnemyType>();

        public override EnemySkillTargetCategory Category => EnemySkillTargetCategory.Enemy;
    }
}
