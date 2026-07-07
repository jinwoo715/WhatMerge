using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class SingleExecution : ExecutionBase
    {
        public SingleExecution(ActiveSkillContext activeContext, SkillCommonContext commonContext) : base(activeContext, commonContext) { }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            yield return SetReadyMotion();
            yield return SetExecutionMotion();

            ShowExecutionVfx();
            List<ICombatant> activeTargets = GetActiveTargets(targets);
            ApplyEffectsToTarget(SearchUtility.GetNearestTarget<ICombatant>(activeTargets, _owner.Position));

            SetIdleMotion();
        }
    }
}
