using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class SkillImpactContext
    {
        public IDamageable ImpactTarget;
        public Vector3 ImpactPosition;
        public SkillImpactContext(IDamageable target, Vector3 position)
        {
            ImpactTarget = target;
            ImpactPosition = position;
        }
    }
}