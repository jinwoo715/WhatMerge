using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "AllAllyEnemyTarget", menuName = "Enemy Skill/Target/All Ally Enemies", order = 2)]
    public sealed class AllAllyEnemyTargetData : EnemySkillTargetData
    {
        public bool IncludeSelf;
        public List<EnemyType> AllowedTypes = new List<EnemyType>();

        public override EnemySkillTargetCategory Category => EnemySkillTargetCategory.Enemy;
    }
}
