using Combat;
using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Projectile
{
    public interface IProjectileMoveStrategy
    {
        event Action OnArrived;
        void Tick(float tick);
    }
}
