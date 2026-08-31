using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class SingleExecution : ExecutionBase
    {
        public SingleExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext) : base(executionContext, runtimeContext) { }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets, float animationTimeScale)
        {
            ICombatant combatant = SelectPrimaryTarget(targets);

            yield return SetReadyMotion(animationTimeScale);
            yield return WaitForCharge();

            ApplyEffectsToTarget(combatant);

            yield return SetExecutionMotion(animationTimeScale, combatant);

            SetIdleMotion();
        }
    }
}
