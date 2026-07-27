using System.Collections.Generic;

namespace Skill.Data
{
    public class SummonOnceExecution : SummonExecutionData
    {
        public List<NormalEffect> Effects;

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
            if (effect is not NormalEffect nomalEffect)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(SummonOnceExecution)} cannot contain {effect?.GetType().Name ?? "null"}. " +
                    $"Expected {nameof(NormalEffect)}.");
            }

            Effects.Add(nomalEffect);
        }

        public override void SetEffects(List<EffectBase> effectBases)
        {
            Effects.Clear();
            foreach (var effect in effectBases)
            {
                if(effect is NormalEffect nomalEffect)
                {
                    Effects.Add(nomalEffect);
                }
            }
        }
    }
}
