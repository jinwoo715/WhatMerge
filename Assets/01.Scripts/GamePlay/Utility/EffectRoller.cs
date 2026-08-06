using Skill.Data;
using System;
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

            for (int i = 0; i < effects.Count; i++)
            {
                EffectBase effect = effects[i];

                if (effect == null)
                    throw new InvalidOperationException($"Effect reference at index {i} is null or missing.");

                if (float.IsNaN(effect.Chance)
                    || float.IsInfinity(effect.Chance)
                    || effect.Chance < 0f
                    || effect.Chance > 1f)
                {
                    throw new InvalidOperationException(
                        $"Effect '{effect.name}' chance must be between 0 and 1. Current value: {effect.Chance}.");
                }

                if (effect.Chance >= 1f
                    || effect.Chance > 0f && UnityEngine.Random.value < effect.Chance)
                {
                    confirmedEffects.Add(effect);
                }
            }

            return confirmedEffects;
        }
    }
}
