using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public abstract class ProjectileDataBase : ScriptableObject, IEffectContainer
    {
        public string Sprite;
        public float Speed;
        public float LifeTime;

        public List<EffectBase> Effects;
        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }
    }
}
