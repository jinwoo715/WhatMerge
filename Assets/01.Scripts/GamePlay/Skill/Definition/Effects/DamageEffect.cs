using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Skill/Effect/DamageEffect", order = 0)]

    public class DamageEffect : EffectBase
    {
        public const string DamageRatioStat = "DamageRatio";

        private static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(DamageRatioStat, "Damage Ratio")
        };

        public float DamageRatio;
        public override void AddStat(string key, float value)
        {
            if(key == DamageRatioStat)
                DamageRatio += value;
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
