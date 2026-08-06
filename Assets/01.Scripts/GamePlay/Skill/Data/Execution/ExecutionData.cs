using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class ExecutionData : ScriptableObject, IEffectContainer
    {
        [Header("이펙트")]
        public List<EffectBase> Effects;

        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }

        public void AddEffect(EffectBase effect)
        {
            Effects.Add(effect);
        }

    }
}
