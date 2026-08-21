using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using WhatMerge.Combat.Effects;
using WhatMerge.Heros;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "TimedBuffEffect", menuName = "Skill/Effect/TimedBuffEffect", order = 0)]
    public class BuffEffect : DurationEffectItem
    {
        public const string ValueKey = "BuffValue";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(ValueKey, "버프 수치")
        };

        public BuffData BuffData;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if(key == ValueKey)
                BuffData.IncreaseRatio += value;
        }
        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
