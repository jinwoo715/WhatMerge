using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class DurationEffect : EffectBase, IEffectContainer
    {
        public float Duration;
        public List<DurationEffectBase> Effects;

        public List<EffectBase> GetEffects => ConvertEffectList();

        public void AddEffect(EffectBase effect)
        {
            if (effect is not DurationEffectBase durationEffect)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(DurationEffect)} cannot contain {effect?.GetType().Name ?? "null"}. " +
                    $"Expected {nameof(DurationEffectBase)}.");
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
                if(effect is DurationEffectBase durationEffectBase)
                {
                    Effects.Add(durationEffectBase);
                }
            }
        }

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}
