using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "KnockBack", menuName = "Skill/Effect/KnockBack", order = 0)]
    public class KnockBackEffect : NormalEffect
    {
        public const string DistanceKey = "DamageRatio";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(DistanceKey, "넉백 거리")
        };

        public float Distance;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == DistanceKey)
                Distance += value;
        }
        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
