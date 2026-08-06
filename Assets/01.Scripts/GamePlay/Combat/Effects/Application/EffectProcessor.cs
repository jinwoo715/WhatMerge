using Skill;
using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace WhatMerge.Combat.Effects
{
    public class EffectProcessor : MonoBehaviour
    {
        private DamageCalculator _damageCalculator;
        private IVFXService _vfx;
        private IReadOnlyList<IEffectHandler> _handlers;
        private IDurationEffectApplier _durationEffectApplier;
        private ITimeEffectService _timeEffectService;
        private IDamageApplier _damageApplier;

        public void Init(
            DamageCalculator damageCalculator,
            IVFXService vfx,
            IReadOnlyList<IEffectHandler> handlers,
            IDurationEffectApplier durationEffectApplier,
            ITimeEffectService timeEffectService,
            IDamageApplier damageApplier)
        {
            _damageCalculator = damageCalculator;
            _vfx = vfx;
            _handlers = handlers;
            _durationEffectApplier = durationEffectApplier;
            _timeEffectService = timeEffectService;
            _damageApplier = damageApplier;
        }

        public void Process(DamageContext damageContext)
        {
            if (damageContext == null)
                throw new ArgumentNullException(nameof(damageContext));

            var effects = EffectRoller.GetConfirmEffects(damageContext.Effects);

            ProcessEffects(effects, damageContext);
        }

        public IRuntimeEffectHandle ApplyPersistentEffects(DamageContext damageContext)
        {
            if (damageContext == null)
                throw new ArgumentNullException(nameof(damageContext));

            List<EffectBase> confirmedEffects = EffectRoller.GetConfirmEffects(damageContext.Effects);
            return ApplyDurationEffects(confirmedEffects, damageContext);
        }
        private void ProcessEffects(List<EffectBase> effects, DamageContext damageContext)
        {
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

                ValidateTargetRequirement(effect, damageContext);
                ShowEffectVFX(effect, damageContext);

                if (TryHandleEffect(effect, damageContext))
                    continue;

                if(effect is DurationEffect durationEffect)
                {
                    ProcessDurationEffect(durationEffect, damageContext);
                }
                else if(effect is KnockBackEffect knockBackEffect)
                {
                    ProcessKnockbackEffect(knockBackEffect, damageContext);
                }
                else if(effect is ExecutionEffect executionEffect)
                {
                    ProcessExecutionEffect(executionEffect, damageContext);
                }
                else if(effect is RangeEffect range)
                {
                    ProcessRangeEffect(range, damageContext);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"No effect handler is registered for {effect.GetType().Name}.");
                }
            }
        }

        private void ProcessRangeEffect(RangeEffect range, DamageContext damageContext)
        {
            if (range.Effects == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RangeEffect)} '{range.name}' has no effect list.");
            }

            var targets = SearchUtility.GetNearEnemies(damageContext.ImpactPosition, range.Range);

            foreach (var target in targets)
            {
                if (target == null || !target.IsActive)
                    continue;

                DamageContext newContext = damageContext.WithTarget(target);
                newContext.Effects = range.Effects;

                List<EffectBase> confirmedEffects = EffectRoller.GetConfirmEffects(range.Effects);
                ProcessEffects(confirmedEffects, newContext);
            }
        }

        private void ProcessExecutionEffect(ExecutionEffect effect, DamageContext context)
        {
            if (context.Target is not IDamageable target)
            {
                throw new InvalidOperationException(
                    $"{nameof(ExecutionEffect)} requires an {nameof(IDamageable)} target. " +
                    $"Received: {context.Target?.GetType().Name ?? "null"}.");
            }

            if (!target.IsActive)
                return;

            float threshold = Mathf.Clamp01(effect.ExecuteThreshold);

            if (target.CurrentHP > target.MaxHP * threshold)
                return;

            _damageApplier.TryApply(
                target,
                target.CurrentHP,
                DamageResultType.ExecutionDamage);
        }

        private void ProcessKnockbackEffect(KnockBackEffect knockBackEffect, DamageContext damageContext)
        {
            if (damageContext.Target is not IDamageable damageable)
            {
                throw new InvalidOperationException(
                    $"{nameof(KnockBackEffect)} requires an {nameof(IDamageable)} target. " +
                    $"Received: {damageContext.Target?.GetType().Name ?? "null"}.");
            }

            if (!damageable.IsActive)
                return;

            damageable.KnockBack(knockBackEffect.Distance);
        }
        private void ProcessDurationEffect(DurationEffect duration, DamageContext damageContext)
        {
            if (float.IsNaN(duration.Duration)
                || float.IsInfinity(duration.Duration)
                || duration.Duration <= 0f)
            {
                throw new InvalidOperationException(
                    $"{nameof(DurationEffect)} duration must be greater than zero. " +
                    $"Current value: {duration.Duration}.");
            }

            if (duration.Effects == null)
                return;

            List<EffectBase> effects = new List<EffectBase>(duration.Effects.Count);

            foreach (DurationEffectItem effect in duration.Effects)
                effects.Add(effect);

            List<EffectBase> confirmedEffects = EffectRoller.GetConfirmEffects(effects);
            IRuntimeEffectHandle handle = ApplyDurationEffects(confirmedEffects, damageContext);

            if (!handle.IsDisposed)
                _timeEffectService.TrackDuration(handle, duration.Duration, damageContext.Target);
        }

        private IRuntimeEffectHandle ApplyDurationEffects(
            IReadOnlyList<EffectBase> effects,
            DamageContext damageContext)
        {
            CompositeRuntimeEffectHandle compositeHandle = new CompositeRuntimeEffectHandle();

            try
            {
                foreach (EffectBase effect in effects)
                {
                    if (effect is not DurationEffectItem durationEffect)
                    {
                        throw new InvalidOperationException(
                            $"Persistent effects can contain only {nameof(DurationEffectItem)}. " +
                            $"Received: {effect?.GetType().Name ?? "null"}.");
                    }

                    ShowEffectVFX(durationEffect, damageContext);
                    compositeHandle.Add(_durationEffectApplier.Apply(durationEffect, damageContext));
                }
            }
            catch
            {
                compositeHandle.Dispose();
                throw;
            }

            if (compositeHandle.Count > 0)
                return compositeHandle;

            compositeHandle.Dispose();
            return RuntimeEffectHandle.Empty;
        }
        private bool TryHandleEffect(EffectBase effect, DamageContext damageContext)
        {
            if (_handlers == null)
                return false;

            foreach (var handler in _handlers)
            {
                if (!handler.CanHandle(effect))
                    continue;

                handler.Handle(effect, damageContext);
                return true;
            }

            return false;
        }
        private void ShowEffectVFX(EffectBase effect, DamageContext damageContext)
        {
            if (effect.VFX == null || damageContext.Attacker == null)
                return;

            _vfx.ShowVFX(effect.VFX, damageContext.ImpactPosition, damageContext.Attacker.Position);
        }

        private static void ValidateTargetRequirement(EffectBase effect, DamageContext damageContext)
        {
            if (damageContext.Target != null
                || effect is RangeEffect
                || effect is GoldEffect)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{effect.GetType().Name} requires a target. " +
                $"Ground-impact effects must contain target-dependent effects inside {nameof(RangeEffect)}.");
        }
    }
}
