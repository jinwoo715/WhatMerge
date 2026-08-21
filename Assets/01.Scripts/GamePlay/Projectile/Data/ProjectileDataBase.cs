using System.Collections.Generic;
using Skill.Data;
using UnityEngine;

namespace WhatMerge.Projectiles.Data
{
    [System.Serializable]
    public enum ProjectileRotateType
    {
        None,
        Rotate
    }

    [System.Serializable]
    public class ProjectileRotate
    {
        public ProjectileRotateType RotateType = ProjectileRotateType.None;
        [Tooltip("Rotation speed in degrees per second. Negative values rotate in the opposite direction.")]
        public float RotateSpeed = 0f;
    }

    public abstract class ProjectileDataBase : ScriptableObject, IEffectContainer
    {
        public string Sprite;
        public float Speed;
        public float LifeTime;
        public float RotationOffset = -90f;
        public ProjectileRotate RotateData = new ProjectileRotate();

        public List<EffectBase> Effects;
        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }

        public void AddEffect(EffectBase effect)
        {
            Effects.Add(effect);
        }
    }
}
