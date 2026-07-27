using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
 

    [CreateAssetMenu(fileName = "Slow", menuName = "Skill/Effect/Slow", order = 0)]
    public class SlowEffect : DurationEffectItem, IEffectStatRevert
    {
        public const string SlowKey = "DamageRatio";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(SlowKey, "이동속도 감소 비율")
        };

        [Range(0, 1)]
        public float SlowRatio;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == SlowKey)
                SlowRatio += value;
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }

        public void Revert()
        {
            SlowRatio *= -1;
        }
    }
}
