using System.Collections.Generic;

namespace Skill.Data
{
    public class SummonOnStayExecution : SummonExecutionData
    {
        public List<DurationEffectBase> Effects;

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
            if (effect is not DurationEffectBase durationEffect)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(SummonOnStayExecution)} cannot contain {effect?.GetType().Name ?? "null"}. " +
                    $"Expected {nameof(DurationEffectBase)}.");
            }

            Effects.Add(durationEffect);
        }

        public override void SetEffects(List<EffectBase> effectBases)
        {
            Effects.Clear();
            foreach (var effect in effectBases)
            {
                if (effect is DurationEffectBase durationEffect)
                {
                    Effects.Add(durationEffect);
                }
            }
        }
    }
}
