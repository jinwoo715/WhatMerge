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
        private readonly bool _isRandom;

        public RandomMultiExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext) : base(executionContext, runtimeContext)
        {
            if (executionContext.ExecutionData is not MultiExecutionData multiExecution)
                throw new System.ArgumentException(
                    "RandomMultiExecution requires MultiExecutionData.",
                    nameof(executionContext));

            _multiCount = multiExecution.MultiCount;
            _isRandom = multiExecution.IsRandom;
        }

        public override IEnumerator Execute(IReadOnlyList<ICombatant> targets, float animationTimeScale)
        {
            List<ICombatant> selectedTargets = SelectTargets(targets);

            yield return SetReadyMotion(animationTimeScale);
            yield return WaitForCharge();

            foreach (ICombatant target in selectedTargets)
            {
                ApplyEffectsToTarget(target);
            }

            ICombatant vfxTarget = selectedTargets.Count > 0 ? selectedTargets[0] : null;
            yield return SetExecutionMotion(animationTimeScale, vfxTarget);

            SetIdleMotion();
        }

        private List<ICombatant> SelectTargets(IReadOnlyList<ICombatant> targets)
        {
            List<ICombatant> candidates = new List<ICombatant>();

            if (targets == null)
                return candidates;

            for (int i = 0; i < targets.Count; i++)
            {
                ICombatant target = targets[i];
                if (target != null && target.IsActive)
                    candidates.Add(target);
            }

            int applyCount = _multiCount > 0 ? Mathf.Min(_multiCount, candidates.Count) : candidates.Count;

            if (_isRandom)
                ShuffleSelection(candidates, applyCount);
            else
                SortByDistance(candidates);

            if (candidates.Count > applyCount)
                candidates.RemoveRange(applyCount, candidates.Count - applyCount);

            return candidates;
        }

        private static void ShuffleSelection(List<ICombatant> candidates, int applyCount)
        {
            for (int i = 0; i < applyCount; i++)
            {
                int randomIndex = Random.Range(i, candidates.Count);
                (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
            }
        }

        private void SortByDistance(List<ICombatant> candidates)
        {
            Vector3 ownerPosition = _owner.Position;
            candidates.Sort((left, right) =>
            {
                float leftDistance = Vector3.SqrMagnitude(left.Position - ownerPosition);
                float rightDistance = Vector3.SqrMagnitude(right.Position - ownerPosition);
                return leftDistance.CompareTo(rightDistance);
            });
        }
    }
}
