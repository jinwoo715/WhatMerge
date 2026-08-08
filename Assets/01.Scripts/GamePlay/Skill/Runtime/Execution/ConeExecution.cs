using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class ConeExecution : ExecutionBase
    {
        private const float DirectionSqrEpsilon = 0.000001f;

        private readonly float _angle;
        private readonly IFinder _finder;

        public ConeExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext)
            : base(executionContext, runtimeContext)
        {
            if (executionContext.ExecutionData is not ConeExecutionData coneExecution)
                throw new ArgumentException("ConeExecution requires ConeExecutionData.", nameof(executionContext));

            if (float.IsNaN(coneExecution.Angle)
                || float.IsInfinity(coneExecution.Angle)
                || coneExecution.Angle <= 0f
                || coneExecution.Angle > 360f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coneExecution.Angle),
                    coneExecution.Angle,
                    "Cone angle must be greater than 0 and at most 360 degrees.");
            }

            _angle = coneExecution.Angle;
            _finder = executionContext.Finder
                ?? throw new ArgumentNullException(nameof(executionContext.Finder));
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            ICombatant pivotTarget = SelectPrimaryTarget(targets);
            Vector3 direction = ResolveCastDirection(pivotTarget);

            yield return SetReadyMotion();

            if (_finder.TryGetTargets(_owner.Position, out IReadOnlyList<ICombatant> impactTargets))
            {
                List<ICombatant> coneTargets = SearchUtility.GetConeTargets<ICombatant>(
                    impactTargets,
                    _owner.Position,
                    direction,
                    _angle);

                foreach (ICombatant target in coneTargets)
                {
                    ApplyEffectsToTarget(target);
                }
            }

            yield return SetExecutionMotion();
            SetIdleMotion();
        }

        private Vector3 ResolveCastDirection(ICombatant pivotTarget)
        {
            Vector3 direction = pivotTarget != null
                ? pivotTarget.Position - _owner.Position
                : _owner.transform.right;

            if (direction.sqrMagnitude <= DirectionSqrEpsilon)
                direction = _owner.transform.right;

            return direction.normalized;
        }
    }
}
