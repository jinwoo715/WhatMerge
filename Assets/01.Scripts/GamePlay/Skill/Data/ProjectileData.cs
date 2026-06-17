using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "Skill/Item/Projectile", order = 0)]
    public class ProjectileData : ScriptableObject
    {
        public string SpriteName;
        public EProjectileMoveType MoveType;
        
        public float Speed;
        public float LifeTime;

        public EProjectileEffectTrigger EffectTrigger;
        public EProjectileEffectTrigger DestroyTrigger;

        public EffectTargetData ResolveData;
    }
}