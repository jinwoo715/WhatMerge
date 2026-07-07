using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill
{
    public class RandomMultiExecution : ExecutionBase
    {
        private readonly int _multiCount;

        public RandomMultiExecution(ActiveSkillContext activeContext, SkillCommonContext commonContext) : base(activeContext, commonContext)
        {
            if (activeContext.Execution is RandomMultiExecutionData randomMultiExecution)
            {
                _multiCount = randomMultiExecution.MultiCount;
            }
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            yield return SetReadyMotion();
            yield return SetExecutionMotion();

            ShowExecutionVfx();

            List<ICombatant> activeTargets = GetActiveTargets(targets);
            int applyCount = _multiCount > 0 ? Mathf.Min(_multiCount, activeTargets.Count) : activeTargets.Count;
            HashSet<int> selectedIndexes = new HashSet<int>();

            while (selectedIndexes.Count < applyCount)
            {
                selectedIndexes.Add(Random.Range(0, activeTargets.Count));
            }

            foreach (int index in selectedIndexes)
            {
                ApplyEffectsToTarget(activeTargets[index]);
            }

            SetIdleMotion();
        }
    }
}