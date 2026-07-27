using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Dot", menuName = "Skill/Effect/Dot", order = 0)]
    public class DotEffect : DurationEffectItem
    {
        public const string ValueStat = "Value";
        public const string IntervalTimeStat = "IntervalTime";

        private static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ValueStat, "적용 수치"),
            new EffectStatDefinition(IntervalTimeStat, "도트 Tick 시간"),
        };

        public float IntervalTime;
        public DotDamageType ApplyType;

        public float Value;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

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
        DamageRatio,
        CurrentHPRatio,
        MaxHPRatio
    }
}
