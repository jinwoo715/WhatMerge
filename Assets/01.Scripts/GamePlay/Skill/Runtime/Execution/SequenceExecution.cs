using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class SequenceExecution : ExecutionBase
    {
        private readonly int _sequenceCount;

        public override float BaseAnimationDuration => base.BaseAnimationDuration * _sequenceCount;

        public SequenceExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext) : base(executionContext, runtimeContext)
        {
            if (executionContext.ExecutionData is not SequenceHitExecutionData sequenceExecution)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(SequenceExecution)} requires {nameof(SequenceHitExecutionData)}.");
            }

            if (sequenceExecution.SequenceCount <= 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(SequenceHitExecutionData.SequenceCount),
                    sequenceExecution.SequenceCount,
                    "Sequence count must be greater than zero.");
            }

            _sequenceCount = sequenceExecution.SequenceCount;
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets, float animationTimeScale)
        {
            ICombatant combatant = SelectPrimaryTarget(targets);

            for (int i = 0; i < _sequenceCount; i++)
            {
                yield return SetReadyMotion(animationTimeScale);

                ApplyEffectsToTarget(combatant);

                yield return SetExecutionMotion(animationTimeScale);
            }

            SetIdleMotion();
        }
    }
}
