using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemyBuffEffect", menuName = "Enemy Skill/Effect/Enemy Buff", order = 0)]
    public sealed class EnemyBuffEffectData : EnemySkillEffectData
    {
        [Min(0f)]
        public float Duration;

        public List<EnemyBuffStatData> Buffs = new List<EnemyBuffStatData>();

        public override EnemySkillEffectTargetType TargetType => EnemySkillEffectTargetType.Enemy;
    }

    [Serializable]
    public sealed class EnemyBuffStatData
    {
        public EnemyStatType StatType;

        [Min(0f)]
        public float FixedIncrease;

        [Min(0f)]
        public float MultiplierIncrease;
    }
}
