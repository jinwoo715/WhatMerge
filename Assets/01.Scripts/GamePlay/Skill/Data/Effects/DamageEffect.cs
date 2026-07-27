using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Skill/Effect/DamageEffect", order = 0)]

    public class DamageEffect : NormalEffect
    {
        public const string DamageRatioStat = "DamageRatio";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(DamageRatioStat, "데미지 적용 비율")
        };

        public float DamageRatio;
        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if(key == DamageRatioStat)
                DamageRatio += value;
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
