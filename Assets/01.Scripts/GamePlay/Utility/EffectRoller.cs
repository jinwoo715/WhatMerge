using Skill.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public static class EffectRoller
    {
        public static List<EffectBase> GetConfirmEffects(List<EffectBase> effects)
        {
            List<EffectBase> confirmedEffects = new List<EffectBase>();

            if (effects == null)
                return confirmedEffects;

            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

                if (effect.Chance >= Random.Range(0f, 1f))
                    confirmedEffects.Add(effect);
            }

            return confirmedEffects;
        }
    }
}