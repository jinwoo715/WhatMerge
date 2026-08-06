using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{


    public class GoldEffect : NormalEffect
    {
        public const string GainGoldAmountKey = "GoldAmount";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(GainGoldAmountKey, "추가 획득 금액")
        };

        public int Gold;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == GainGoldAmountKey)
                Gold += (int)value;
        }
        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
