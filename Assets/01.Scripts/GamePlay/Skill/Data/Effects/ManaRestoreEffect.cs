using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ManaRestoreEffect", menuName = "Skill/Effect/ManaRestoreEffect", order = 0)]
    public class ManaRestoreEffect : NormalEffect
    {
        public const string ManaAmountKey = "ManaAmount";

        private static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(ManaAmountKey, "마나 회복량")
        };

        [Min(0f)]
        public float ManaAmount;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == ManaAmountKey)
                ManaAmount = Mathf.Max(0f, ManaAmount + value);
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
