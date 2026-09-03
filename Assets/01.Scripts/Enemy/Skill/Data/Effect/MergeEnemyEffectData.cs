using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "MergeEnemyEffect", menuName = "Enemy Skill/Effect/Merge Enemy", order = 6)]
    public sealed class MergeEnemyEffectData : EnemySkillEffectData
    {
        [Min(1)]
        public int ResultEnemyUID;

        public override EnemySkillEffectTargetType TargetType => EnemySkillEffectTargetType.Enemy;
    }
}
