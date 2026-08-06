using System.Collections.Generic;
using Skill.Data;

namespace WhatMerge.Summons.Data
{
    public class SummonOnStayExecution : SummonExecutionData
    {
        public List<DurationEffectItem> Effects;

        public override List<EffectBase> GetEffectList()
        {
            List<EffectBase> effectBases = new List<EffectBase>(Effects.Count);

            foreach (var effect in Effects)
            {
                effectBases.Add(effect);
            }

            return effectBases;
        }
        public override void AddEffect(EffectBase effect)
        {
            if (effect is not DurationEffectItem durationEffect)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(SummonOnStayExecution)} cannot contain {effect?.GetType().Name ?? "null"}. " +
                    $"Expected {nameof(DurationEffectItem)}.");
            }

            Effects.Add(durationEffect);
        }

        public override void SetEffects(List<EffectBase> effectBases)
        {
            Effects.Clear();
            foreach (var effect in effectBases)
            {
                if (effect is DurationEffectItem durationEffect)
                {
                    Effects.Add(durationEffect);
                }
            }
        }
    }
}
