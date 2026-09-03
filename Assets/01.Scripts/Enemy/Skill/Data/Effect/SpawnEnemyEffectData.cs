using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    public enum EnemySpawnPositionType
    {
        PathStart,
        Owner,
        Target,
        AroundOwner,
        RelativeToOwnerPath
    }

    [CreateAssetMenu(fileName = "SpawnEnemyEffect", menuName = "Enemy Skill/Effect/Spawn Enemy", order = 4)]
    public sealed class SpawnEnemyEffectData : EnemySkillEffectData
    {
        [Min(1)]
        public int EnemyUID;

        [Min(1)]
        public int Count = 1;

        [Min(0f)]
        public float SpawnInterval;

        public EnemySpawnPositionType SpawnPositionType;

        [Min(0f)]
        public float AroundOwnerRadius;

        [Tooltip("Positive values spawn ahead of the owner and negative values spawn behind it.")]
        public float PathDistanceOffset;

        public override bool RequiresTarget => SpawnPositionType == EnemySpawnPositionType.Target;
        public override EnemySkillEffectTargetType TargetType => EnemySkillEffectTargetType.Any;
    }
}
