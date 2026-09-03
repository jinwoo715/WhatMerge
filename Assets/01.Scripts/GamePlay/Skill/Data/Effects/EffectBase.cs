using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public abstract class EffectBase : ScriptableObject, IEffectValueModifier
    {
        private static readonly EffectStatDefinition[] EmptyEnhanceableStats = { };

        public static readonly string ChanceKey = "Chance";
        public long RuntimeEffectInstanceId { get; internal set; }


        [Range(0, 1)]
        public float Chance = 1f;

        [Header("적용 효과 아이콘")]
        public VFXData VFX;

        public void AddChance(float value)
        {
            Chance += value;

            Chance = Mathf.Min(Chance, 1);
        }

        public virtual void AddStat(string key, float value)
        {
            if (key == ChanceKey)
                Chance += value;
        }

        public virtual IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EmptyEnhanceableStats;
        }
    }

    public static class EffectTargetPolicy
    {
        public static bool RequiresDirectTarget(EffectBase effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            return effect is not RangeEffect
                and not GoldEffect
                and not SummonSpawnEffect;
        }
    }
}
