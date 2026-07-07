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
        private readonly float _tickTime;

        public SequenceExecution(ActiveSkillContext activeContext, SkillCommonContext commonContext) : base(activeContext, commonContext)
        {
            if (activeContext.Execution is SequenceHitExecutionData sequenceExecution)
            {
                _sequenceCount = sequenceExecution.SequenceCount;
                _tickTime = sequenceExecution.TickTime;
            }
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            yield return SetReadyMotion();
            yield return SetExecutionMotion();

            ShowExecutionVfx();

            int count = Mathf.Max(1, _sequenceCount);
            List<ICombatant> activeTargets = GetActiveTargets(targets);
            for (int i = 0; i < count; i++)
            {
                ApplyEffectsToTarget(SearchUtility.GetNearestTarget<ICombatant>(activeTargets, _owner.Position));

                if (_tickTime > 0f && i < count - 1)
                {
                    yield return new WaitForSeconds(_tickTime);
                }
            }

            SetIdleMotion();
        }
    }
}
