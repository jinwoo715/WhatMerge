using Combat;
using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Projectile
{
    public interface IProjectileMoveStrategy
    {
        event Action<SkillImpactContext> OnArrived;
        bool IsArrived { get; }
        void Init(Transform owner, ICombatant target, float speed);
        void Tick();
    }
}
