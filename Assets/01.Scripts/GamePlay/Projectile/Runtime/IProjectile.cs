using System;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;

namespace WhatMerge.Projectiles
{
    public readonly struct ProjectileImpact
    {
        public ICombatant Target { get; }
        public Vector3 Position { get; }

        public ProjectileImpact(ICombatant target, Vector3 position)
        {
            Target = target;
            Position = position;
        }
    }

    public interface IProjectile
    {
        event Action<ProjectileImpact> OnExecute;
        event Action OnExpired;
        void Tick(float tick);
        void HitTarget(ICombatant target);
    }
}
