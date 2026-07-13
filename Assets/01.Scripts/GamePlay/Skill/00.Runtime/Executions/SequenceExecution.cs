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

        public SequenceExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext) : base(executionContext, runtimeContext)
        {
            if (executionContext.ExecutionData is SequenceHitExecutionData sequenceExecution)
            {
                _sequenceCount = sequenceExecution.SequenceCount;
            }
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            ICombatant combatant = NearestTarget(targets);

            for (int i = 0; i < _sequenceCount; i++)
            {
                yield return SetReadyMotion();

                ShowExecutionVfx();

                ApplyEffectsToTarget(combatant);

                yield return SetExecutionMotion();
            }

            SetIdleMotion();
        }
    }
}
