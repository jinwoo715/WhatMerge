using System.Collections.Generic;
using Skill.Data;
using UnityEngine;

namespace WhatMerge.Projectiles.Data
{
    public abstract class ProjectileDataBase : ScriptableObject, IEffectContainer
    {
        public string Sprite;
        public float Speed;
        public float LifeTime;

        public List<EffectBase> Effects;
        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }

        public void AddEffect(EffectBase effect)
        {
            Effects.Add(effect);
        }
    }
}
