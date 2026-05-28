using Skill.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public static class EffectRoller
    {
        public static List<EffectBase> GetConfirmEffects(List<EffectEntry> effects)
        {
            List<EffectBase> confirmedEffects = new List<EffectBase>();

            foreach (var effect in effects)
            {
                float chance = UnityEngine.Random.Range(0f, 1);

                if (effect.Chance >= chance)
                {
                    confirmedEffects.Add(effect.Effect);
                }
            }

            return confirmedEffects;
        }
    }
}