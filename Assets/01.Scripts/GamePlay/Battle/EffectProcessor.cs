using Skill;
using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;

namespace WhatMerge.Combat
{
    public class EffectProcessor : MonoBehaviour
    {
        private DamageCalculator _damageCalculator;
        private IVFXService _vfx;
        private IDotService _dotService;
        private IReadOnlyList<IEffectHandler> _handlers;
        private ITimeEffectService _timeEffectService;
        private IDamageApplier _damageApplier;
        public void Init(DamageCalculator damageCalculator, IVFXService vfx, IReadOnlyList<IEffectHandler> handlers, IDotService dotService, ITimeEffectService timeEffectService, IDamageApplier damageApplier)
        {
            _damageCalculator = damageCalculator;
            _dotService = dotService;
            _vfx = vfx;
            _handlers = handlers;
            _timeEffectService = timeEffectService;
            _damageApplier = damageApplier;
        }
        public void Process(DamageContext damageContext)
        {
            var effects = EffectRoller.GetConfirmEffects(damageContext.Effects);

            ProcessEffects(effects, damageContext);
        }
        private void ProcessEffects(List<EffectBase> effects, DamageContext damageContext)
        {
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

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
            }
        }

        private void ProcessExecutionEffect(ExecutionEffect effect, DamageContext context)
        {
            if (context.Target is not IDamageable target || !target.IsActive)
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
            if(damageContext.Target is IDamageable damageable)
            {
                damageable.KnockBack(knockBackEffect.Diatance);
            }
        }
        private void ProcessDurationEffect(DurationEffect duration, DamageContext damageContext)
        {
            if (duration.Effects == null)
                return;

            foreach (DurationEffectBase effect in duration.Effects)
            {
                switch (effect)
                {
                    case DotEffect dot:
                        _dotService.ApplyDotEffect(new DotData(duration.Duration, dot, damageContext));
                        break;
                    case SlowEffect slow:
                        _timeEffectService.ApplySlow(duration.Duration, slow.SlowRatio, damageContext.Target);
                        break;
                    case StunEffect:
                        _timeEffectService.ApplyStun(duration.Duration, damageContext.Target);
                        break;
                    case ElementEffect element:
                        _timeEffectService.ApplyElement(duration.Duration, damageContext.Target, element.Element);
                        break;
                    case ArmorReductionEffect armorReduction:
                        _timeEffectService.ApplyArmorReduction(duration.Duration, armorReduction.Value, damageContext.Target);
                        break;
                    case DamageTransferEffect damageTransfer:
                        _timeEffectService.ApplyDamageTransfer(_damageApplier, duration.Duration, damageContext.Target, damageTransfer);
                        break;
                }
            }
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
            if (_vfx == null || effect.VFX == null || damageContext.Target == null || damageContext.Attacker == null)
                return;

            _vfx.ShowEffect(effect.VFX.VFXName, damageContext.Target.Position, damageContext.Attacker.Position);
        }
    }
}
