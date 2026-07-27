using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DamageTransfer", menuName = "Skill/Effect/DamageTransfer", order = 0)]
    public class DamageTransferEffect : DurationEffectItem
    {
        public const string TransitionRadiusKey = "TransitionRadius";
        public const string TransitionCountKey = "TransitionCount";
        public const string TransitionRatioKey = "TransitionRatio";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(TransitionRadiusKey, "전이 범위"),
            new EffectStatDefinition(TransitionCountKey, "전이 숫자"),
            new EffectStatDefinition(TransitionRatioKey, "전이 수치")
        };

        public float Radius;
        public int Count;

        [Range(0, 1)]
        public float TransitionRatio;

        public override void AddStat(string key, float value) 
        {
            base.AddStat(key, value);

            switch (key)
            {
                case TransitionRadiusKey:
                    Radius += value;
                    break;
                case TransitionCountKey:
                    Count += (int)value;
                    break;
                case TransitionRatioKey:
                    TransitionRatio += value;
                    break;
            }
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}