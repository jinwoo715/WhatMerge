using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Heros;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "HeroDebuffEffect", menuName = "Enemy Skill/Effect/Hero Debuff", order = 1)]
    public sealed class HeroDebuffEffectData : EnemySkillEffectData
    {
        [Min(0f)]
        public float Duration;

        public List<HeroDebuffStatData> Debuffs = new List<HeroDebuffStatData>();

        public override EnemySkillEffectTargetType TargetType => EnemySkillEffectTargetType.Hero;
    }

    [Serializable]
    public sealed class HeroDebuffStatData
    {
        public HeroStatType StatType;

        [Min(0f)]
        public float FixedReduction;

        [Min(0f)]
        public float MultiplierReduction;
    }
}
