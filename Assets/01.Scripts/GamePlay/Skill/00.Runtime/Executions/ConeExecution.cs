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

        public ConeExecution(ActiveSkillContext activeContext, SkillCommonContext commonContext) : base(activeContext, commonContext)
        {
            if (activeContext.Execution is ConeExecutionData coneExecution)
            {
                _angle = coneExecution.Angle;
            }
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            yield return SetReadyMotion();

            List<ICombatant> activeTargets = GetActiveTargets(targets);
            ICombatant pivotTarget = SearchUtility.GetNearestTarget<ICombatant>(activeTargets, _owner.Position);
            Vector3 direction = pivotTarget != null ? (pivotTarget.Position - _owner.Position).normalized : _owner.transform.right;

            yield return SetExecutionMotion();

            ShowExecutionVfx();
            ApplyEffectsToTargets(SearchUtility.GetConeTargets<ICombatant>(activeTargets, _owner.Position, direction, _angle));

            SetIdleMotion();
        }
    }
}