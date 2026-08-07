using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class ConeExecution : ExecutionBase
    {
        private readonly float _angle;

        public ConeExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext) : base(executionContext, runtimeContext)
        {
            if (executionContext.ExecutionData is ConeExecutionData coneExecution)
            {
                _angle = coneExecution.Angle;
            }
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            ICombatant pivotTarget = SelectPrimaryTarget(targets);

            Vector3 direction = pivotTarget != null ? (pivotTarget.Position - _owner.Position).normalized : _owner.transform.right;

            List<ICombatant> coneTargetList = SearchUtility.GetConeTargets<ICombatant>(targets, _owner.Position, direction, _angle);

            yield return SetReadyMotion();

            foreach (var combatant in coneTargetList)
            {
                ApplyEffectsToTarget(combatant);
            }

            yield return SetExecutionMotion();

            SetIdleMotion();
        }
    }
}
