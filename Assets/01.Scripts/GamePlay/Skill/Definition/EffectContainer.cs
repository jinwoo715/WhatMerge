using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public interface IEffectContainer
    {
        public List<EffectBase> GetEffects { get; }
        public void AddEffect(EffectBase effect);
    }
}
