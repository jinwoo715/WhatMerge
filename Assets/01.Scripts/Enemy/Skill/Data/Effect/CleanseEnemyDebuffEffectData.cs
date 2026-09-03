using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "CleanseEnemyDebuffEffect", menuName = "Enemy Skill/Effect/Cleanse Enemy Debuff", order = 3)]
    public sealed class CleanseEnemyDebuffEffectData : EnemySkillEffectData
    {
        [Tooltip("Zero removes every cleansable debuff.")]
        [Min(0)]
        public int MaxCleanseCount;

        public EnemyStatusRemovalPolicy Policy;

        public override EnemySkillEffectTargetType TargetType => EnemySkillEffectTargetType.Enemy;
    }
}
