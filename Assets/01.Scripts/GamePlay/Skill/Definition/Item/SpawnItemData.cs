using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public abstract class SpawnItemData : ScriptableObject, IEffectContainer
    {
        public List<EffectBase> Effects;
        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }
    }
}
