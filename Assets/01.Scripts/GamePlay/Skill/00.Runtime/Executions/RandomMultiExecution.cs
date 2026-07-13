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

        public RandomMultiExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext) : base(executionContext, runtimeContext)
        {
            if (executionContext.ExecutionData is RandomMultiExecutionData randomMultiExecution)
            {
                _multiCount = randomMultiExecution.MultiCount;
            }
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets)
        {
            HashSet<int> randomIndex = GetRandomCombatantIndex(targets);

            yield return SetReadyMotion();

            ShowExecutionVfx();

            foreach (int index in randomIndex)
            {
                ApplyEffectsToTarget(targets[index]);
            }

            yield return SetExecutionMotion();

            SetIdleMotion();
        }

        private HashSet<int> GetRandomCombatantIndex(IReadOnlyList<ICombatant> targets)
        {
            int applyCount = _multiCount > 0 ? Mathf.Min(_multiCount, targets.Count) : targets.Count;
            HashSet<int> selectedIndexes = new HashSet<int>();

            while (selectedIndexes.Count < applyCount)
            {
                selectedIndexes.Add(Random.Range(0, targets.Count));
            }

            return selectedIndexes;
        }
    }
}
