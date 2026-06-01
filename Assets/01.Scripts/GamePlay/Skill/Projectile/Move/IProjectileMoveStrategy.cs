using Combat;
using System;
using UnityEngine;

namespace Skill.Projectile
{
    public interface IProjectileMoveStrategy
    {
        event Action<SkillImpactContext> OnArrived;
        bool IsArrived { get; }
        void Init(Transform owner, ICreature target, float speed);
        void Tick();
    }
}