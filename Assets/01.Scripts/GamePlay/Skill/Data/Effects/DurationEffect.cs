using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class DurationEffect : EffectBase, IEffectContainer
    {
        public const string DurationKey = "EffectDuration";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(DurationKey, "지속 시간")
        };

        public float Duration;
        public List<DurationEffectItem> Effects;

        public List<EffectBase> GetEffects => ConvertEffectList();

        public void AddEffect(EffectBase effect)
        {
            if (effect is not DurationEffectItem durationEffect)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(DurationEffect)} cannot contain {effect?.GetType().Name ?? "null"}. " +
                    $"Expected {nameof(DurationEffectItem)}.");
            }

            Effects.Add(durationEffect);
        }

        public List<EffectBase> ConvertEffectList()
        {
            List<EffectBase> list = new List<EffectBase>();
            foreach (var effect in Effects)
            {
                list.Add(effect);
            }

            return list;
        }
        public void SetEffectList(List<EffectBase> effectBases)
        {
            Effects.Clear();
            foreach (var effect in effectBases)
            {
                if(effect is DurationEffectItem durationEffectBase)
                {
                    Effects.Add(durationEffectBase);
                }
            }
        }

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == DurationKey)
                Duration += value;
        }
        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
