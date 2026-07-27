using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DecreaseAmour", menuName = "Skill/Effect/DecreaseAmour", order = 0)]
    public class ArmorReductionEffect : DurationEffectItem
    {
        public const string ArmorReductionKey = "ArmorReduction";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(ArmorReductionKey, "방어력 감소 수치")
        };

        [Range(0,1)]
        public float ReductionValue;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == ArmorReductionKey)
                ReductionValue += value;
        }
        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
