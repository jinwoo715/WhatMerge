using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{


    public class GoldEffect : NormalEffect
    {
        public const string GainGoldAmountKey = "GoldAmount";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "¹ßµ¿È®·ü"),
            new EffectStatDefinition(GainGoldAmountKey, "Ãß°¡ È¹µæ ±Ý¾×")
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
