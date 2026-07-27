using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class RangeEffect : NormalEffect, IEffectContainer
    {
        public const string RangeKey = "DamageRatio";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(RangeKey, "데미지 적용 범위")
        };

        public float Range;
        public List<EffectBase> Effects;
        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }

        public void AddEffect(EffectBase effect)
        {
            Effects.Add(effect);
        }

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == RangeKey)
                Range += value;
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
