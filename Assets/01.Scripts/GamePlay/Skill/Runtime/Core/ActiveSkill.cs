using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace Skill
{
    public class ActiveSkill : IActiveSkill
    {
        private Hero _owner;
        private IReadOnlyList<ICombatant> _cachedTargets = Array.Empty<ICombatant>();

        public ITrigger Trigger { get; private set; }
        public IFinder Target { get; private set; }
        public IExecute Execution { get; private set; }
        public float BaseAnimationDuration => Execution.BaseAnimationDuration;
        public float ChargeTime => Execution.ChargeTime;
        public float ActivationChance { get; private set; }

        public int SkillUID { get; }
        public int SpawnIndex { get; }

        public event Action OnDispose;

        public ActiveSkill(
            int uid,
            Hero owner,
            ITrigger trigger,
            IFinder search,
            IExecute excution,
            float activationChance)
        {
            SkillUID = uid;
            _owner = owner;
            Trigger = trigger;
            Target = search;
            Execution = excution;
            SpawnIndex = owner.SpawnIndex;
            ActivationChance = ValidateActivationChance(activationChance);
        }
        public bool IsUsable(SkillTriggerContext context)
        {
            _cachedTargets = Array.Empty<ICombatant>();

            if (!Trigger.IsMeetTrigger(context))
            {
                return false;
            }

            return Target.TryGetTargets(_owner.Position, out _cachedTargets);
        }

        public bool RollActivation()
        {
            return ActivationChance >= 1f
                || ActivationChance > 0f && UnityEngine.Random.value < ActivationChance;
        }

        public void AddActivationChance(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Activation chance modifier must be a finite number.");
            }

            ActivationChance = Mathf.Clamp01(ActivationChance + value);
        }

        public IEnumerator Execute(float animationTimeScale)
        {
            IReadOnlyList<ICombatant> targets = _cachedTargets;
            _cachedTargets = Array.Empty<ICombatant>();
            yield return Execution.Execute(targets, animationTimeScale);
        }

        public void Dispose()
        {
            _cachedTargets = Array.Empty<ICombatant>();
            OnDispose?.Invoke();
        }

        private static float ValidateActivationChance(float value)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value < 0f
                || value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Activation chance must be between zero and one.");
            }

            return value;
        }
    }
}
