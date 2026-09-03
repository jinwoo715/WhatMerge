using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    public enum EnemyStatusRemovalPolicy
    {
        OldestFirst,
        NewestFirst
    }

    [CreateAssetMenu(fileName = "DispelHeroBuffEffect", menuName = "Enemy Skill/Effect/Dispel Hero Buff", order = 2)]
    public sealed class DispelHeroBuffEffectData : EnemySkillEffectData
    {
        [Tooltip("Zero removes every dispellable buff.")]
        [Min(0)]
        public int MaxDispelCount;

        public EnemyStatusRemovalPolicy Policy;

        public override EnemySkillEffectTargetType TargetType => EnemySkillEffectTargetType.Hero;
    }
}
