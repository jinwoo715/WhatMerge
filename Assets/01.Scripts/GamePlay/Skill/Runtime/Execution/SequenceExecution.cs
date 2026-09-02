using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class SequenceExecution : ExecutionBase, ISequenceCountModifier
    {
        private int _sequenceCount;

        public override float BaseAnimationDuration => base.BaseAnimationDuration;

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

        public void AddSequenceCount(int count)
        {
            if (count <= 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "Sequence count increment must be greater than zero.");
            }

            try
            {
                _sequenceCount = checked(_sequenceCount + count);
            }
            catch (System.OverflowException exception)
            {
                throw new System.InvalidOperationException(
                    $"Sequence count overflow. Current: {_sequenceCount}, Add: {count}.",
                    exception);
            }
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets, float animationTimeScale)
        {
            ICombatant combatant = SelectPrimaryTarget(targets);
            int sequenceCount = _sequenceCount;

            float perTimeScale = animationTimeScale / sequenceCount;

            for (int i = 0; i < sequenceCount; i++)
            {
                yield return SetReadyMotion(perTimeScale);

                if (i == 0)
                    yield return WaitForCharge();

                ApplyEffectsToTarget(combatant);

                yield return SetExecutionMotion(perTimeScale, combatant);
            }

            SetIdleMotion();
        }
    }
}
