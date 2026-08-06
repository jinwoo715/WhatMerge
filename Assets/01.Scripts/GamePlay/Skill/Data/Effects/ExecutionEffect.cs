using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class ExecutionEffect : EffectBase
    {
        public const string ExecuteKey = "ExecuteRatio";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(ExecuteKey, "처형 기준")
        };

        //%이하 처형
        [Range(0,1)]
        public float ExecuteThreshold;
        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (ExecuteKey == key)
                ExecuteThreshold += value;
        }
        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
