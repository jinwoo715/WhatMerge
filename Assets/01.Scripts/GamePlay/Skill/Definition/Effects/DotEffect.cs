using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Dot", menuName = "Skill/Effect/Dot", order = 0)]
    public class DotEffect : DurationEffectBase
    {
        public const string ValueStat = "Value";
        public const string IntervalTimeStat = "IntervalTime";

        private static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ValueStat, "Value"),
            new EffectStatDefinition(IntervalTimeStat, "Interval Time"),
        };

        public float IntervalTime;
        public DotDamageType ApplyType;

        public float Value;

        public override void AddStat(string key, float value)
        {
            switch (key)
            {
                case ValueStat:
                    Value += value;
                    break;
                case IntervalTimeStat:
                    IntervalTime += value;
                    break;
            }
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
    public enum DotDamageType
    {
        Fixed,
        CurrentHPRatio,
        MaxHPRatio
    }
}
