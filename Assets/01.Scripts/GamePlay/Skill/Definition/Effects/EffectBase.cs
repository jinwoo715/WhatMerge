using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public abstract class EffectBase : ScriptableObject, IEffectValueModifier
    {
        private static readonly EffectStatDefinition[] EmptyEnhanceableStats = { };

        [Range(0, 1)]
        public float Chance = 1f;

        [Header("적용 효과 아이콘")]
        public VFXData VFX;

        public void AddChance(float value)
        {
            Chance += value;

            Chance = Mathf.Min(Chance, 1);
        }

        public abstract void AddStat(string key, float value);

        public virtual IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EmptyEnhanceableStats;
        }
    }
}
