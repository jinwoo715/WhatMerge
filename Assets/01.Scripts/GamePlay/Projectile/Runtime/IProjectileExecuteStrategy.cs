using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Projectiles
{
    public interface IProjectileExecuteStrategy
    {
        void OnTrigger(EProjectileEffectTrigger trigger);
    }
}
