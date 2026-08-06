using Skill;
using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;
using WhatMerge.Projectiles;
using WhatMerge.Projectiles.Data;
using WhatMerge.Summons;
using WhatMerge.Summons.Data;

namespace WhatMerge.Combat.Effects
{
    public interface IEffectHandler
    {
        bool CanHandle(EffectBase effect);
        void Handle(EffectBase effect, DamageContext damageContext);
    }

    public interface IRuntimeEffectHandle : IDisposable
    {
        bool IsDisposed { get; }
    }

    public sealed class RuntimeEffectHandle : IRuntimeEffectHandle
    {
        private sealed class EmptyRuntimeEffectHandle : IRuntimeEffectHandle
        {
            public bool IsDisposed => true;
            public void Dispose() { }
        }

        public static IRuntimeEffectHandle Empty { get; } = new EmptyRuntimeEffectHandle();

        private Action _release;
        public bool IsDisposed { get; private set; }

        public RuntimeEffectHandle(Action release)
        {
            _release = release ?? throw new ArgumentNullException(nameof(release));
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            Action release = _release;
            _release = null;
            release.Invoke();
        }
    }

    public sealed class CompositeRuntimeEffectHandle : IRuntimeEffectHandle
    {
        private readonly List<IRuntimeEffectHandle> _handles = new();

        public bool IsDisposed { get; private set; }
        public int Count => _handles.Count;

        public void Add(IRuntimeEffectHandle handle)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            if (IsDisposed)
            {
                handle.Dispose();
                throw new ObjectDisposedException(nameof(CompositeRuntimeEffectHandle));
            }

            if (!handle.IsDisposed)
                _handles.Add(handle);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            Exception firstException = null;

            for (int i = _handles.Count - 1; i >= 0; i--)
            {
                try
                {
                    _handles[i].Dispose();
                }
                catch (Exception exception)
                {
                    firstException ??= exception;
                }
            }

            _handles.Clear();

            if (firstException != null)
                throw firstException;
        }
    }

    public interface IDurationEffectHandler
    {
        bool CanHandle(DurationEffectItem effect);
        IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext);
    }

    public interface IDurationEffectApplier
    {
        IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext);
    }

    public sealed class DurationEffectApplier : IDurationEffectApplier
    {
        private readonly IReadOnlyList<IDurationEffectHandler> _handlers;

        public DurationEffectApplier(IReadOnlyList<IDurationEffectHandler> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));
            if (damageContext == null)
                throw new ArgumentNullException(nameof(damageContext));

            IDurationEffectHandler matchedHandler = null;

            foreach (IDurationEffectHandler handler in _handlers)
            {
                if (!handler.CanHandle(effect))
                    continue;

                if (matchedHandler != null)
                {
                    throw new InvalidOperationException(
                        $"Multiple duration effect handlers are registered for {effect.GetType().Name}.");
                }

                matchedHandler = handler;
            }

            if (matchedHandler == null)
            {
                throw new InvalidOperationException(
                    $"No duration effect handler is registered for {effect.GetType().Name}.");
            }

            return matchedHandler.Apply(effect, damageContext)
                ?? throw new InvalidOperationException(
                    $"{matchedHandler.GetType().Name} returned a null runtime effect handle.");
        }
    }

    public sealed class DotDurationEffectHandler : IDurationEffectHandler
    {
        private readonly IDotService _dotService;

        public DotDurationEffectHandler(IDotService dotService)
        {
            _dotService = dotService;
        }

        public bool CanHandle(DurationEffectItem effect) => effect is DotEffect;

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            return _dotService.ApplyDotEffect(new DotData((DotEffect)effect, damageContext));
        }
    }

    public sealed class SlowDurationEffectHandler : IDurationEffectHandler
    {
        private readonly ITimeEffectService _timeEffectService;

        public SlowDurationEffectHandler(ITimeEffectService timeEffectService)
        {
            _timeEffectService = timeEffectService;
        }

        public bool CanHandle(DurationEffectItem effect) => effect is SlowEffect;

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            return _timeEffectService.ApplySlow(((SlowEffect)effect).SlowRatio, damageContext.Target);
        }
    }

    public sealed class StunDurationEffectHandler : IDurationEffectHandler
    {
        private readonly ITimeEffectService _timeEffectService;

        public StunDurationEffectHandler(ITimeEffectService timeEffectService)
        {
            _timeEffectService = timeEffectService;
        }

        public bool CanHandle(DurationEffectItem effect) => effect is StunEffect;

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            return _timeEffectService.ApplyStun(damageContext.Target);
        }
    }

    public sealed class ElementDurationEffectHandler : IDurationEffectHandler
    {
        private readonly ITimeEffectService _timeEffectService;

        public ElementDurationEffectHandler(ITimeEffectService timeEffectService)
        {
            _timeEffectService = timeEffectService;
        }

        public bool CanHandle(DurationEffectItem effect) => effect is ElementEffect;

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            return _timeEffectService.ApplyElement(damageContext.Target, ((ElementEffect)effect).Element);
        }
    }

    public sealed class ArmorReductionDurationEffectHandler : IDurationEffectHandler
    {
        private readonly ITimeEffectService _timeEffectService;

        public ArmorReductionDurationEffectHandler(ITimeEffectService timeEffectService)
        {
            _timeEffectService = timeEffectService;
        }

        public bool CanHandle(DurationEffectItem effect) => effect is ArmorReductionEffect;

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            return _timeEffectService.ApplyArmorReduction(
                ((ArmorReductionEffect)effect).ReductionValue,
                damageContext.Target);
        }
    }

    public sealed class DamageTransferDurationEffectHandler : IDurationEffectHandler
    {
        private readonly ITimeEffectService _timeEffectService;
        private readonly IDamageApplier _damageApplier;

        public DamageTransferDurationEffectHandler(
            ITimeEffectService timeEffectService,
            IDamageApplier damageApplier)
        {
            _timeEffectService = timeEffectService;
            _damageApplier = damageApplier;
        }

        public bool CanHandle(DurationEffectItem effect) => effect is DamageTransferEffect;

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            return _timeEffectService.ApplyDamageTransfer(
                _damageApplier,
                damageContext.Target,
                (DamageTransferEffect)effect);
        }
    }

    public sealed class BuffDurationEffectHandler : IDurationEffectHandler
    {
        private readonly IBuffService _buffService;

        public BuffDurationEffectHandler(IBuffService buffService)
        {
            _buffService = buffService;
        }

        public bool CanHandle(DurationEffectItem effect) => effect is BuffEffect;

        public IRuntimeEffectHandle Apply(DurationEffectItem effect, DamageContext damageContext)
        {
            if (damageContext.Target is not Hero hero)
            {
                throw new InvalidOperationException(
                    $"{nameof(BuffEffect)} requires a {nameof(Hero)} target. " +
                    $"Received: {damageContext.Target?.GetType().Name ?? "null"}.");
            }

            if (!hero.IsActive)
                return RuntimeEffectHandle.Empty;

            return _buffService.ApplyBuff((BuffEffect)effect, hero.StatModify);
        }
    }

    public interface IApplyDamageNotifier
    {
        event Action<Vector3, int> OnApplyDamage;
    }

    public class DamageEffectHandler : IEffectHandler, IApplyDamageNotifier
    {
        private readonly DamageCalculator _damageCalculator;
        private IDamageApplier _damageApplier;

        public event Action<Vector3, int> OnApplyDamage;

        public DamageEffectHandler(DamageCalculator damageCalculator, IDamageApplier damageApplier)
        {
            _damageCalculator = damageCalculator;
            _damageApplier = damageApplier;
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is DamageEffect;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (effect is not DamageEffect damageEffect)
            {
                throw new InvalidOperationException(
                    $"{nameof(DamageEffectHandler)} cannot handle {effect?.GetType().Name ?? "null"}.");
            }

            if (damageContext.Target is not IDamageable damageable)
            {
                throw new InvalidOperationException(
                    $"{nameof(DamageEffect)} requires an {nameof(IDamageable)} target. " +
                    $"Received: {damageContext.Target?.GetType().Name ?? "null"}.");
            }

            if (!damageable.IsActive)
                return;

            int appliedDamage = _damageCalculator.CalculateFinalDamage(
                damageable,
                damageContext.AttackPayload,
                damageEffect.DamageRatio,
                damageEffect.Attribute);
            _damageApplier.TryApply(damageable, appliedDamage);
        }

    }
    public class SummonSpawnEffectHandler : IEffectHandler
    {
        private readonly ISummonProvider _summonProvider;

        public SummonSpawnEffectHandler(ISummonProvider summonProvider)
        {
            _summonProvider = summonProvider;
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is SummonSpawnEffect;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            List<EffectBase> effects = new List<EffectBase>();

            if(effect is SummonSpawnEffect spawnEffect)
            {
                if(spawnEffect.Execution is SummonOnceExecution once)
                {
                    effects = new List<EffectBase>(once.Effects);
                }
                else if(spawnEffect.Execution is SummonOnStayExecution stay)
                {
                    effects = new List<EffectBase>(stay.Effects);
                }
            }

            if (effect is SummonSpawnEffect summonSpawnEffect)
            {
                DamageContext context = damageContext.WithEffects(effects);

                _summonProvider.SpawnSummon(summonSpawnEffect, context);
            }
        }
    }
    public class ProjectileSpawnEffectHandler : IEffectHandler
    {
        private readonly IProjectileProvider _projectileProvider;

        public ProjectileSpawnEffectHandler(IProjectileProvider projectileProvider)
        {
            _projectileProvider = projectileProvider
                ?? throw new ArgumentNullException(nameof(projectileProvider));
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is ProjectileSpawnEffect projectileSpawnEffect && projectileSpawnEffect.Projectile != null;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (effect is ProjectileSpawnEffect projectileSpawnEffect)
            {
                SpawnProjectile(projectileSpawnEffect.Projectile, damageContext);
                return;
            }
        }

        private void SpawnProjectile(ProjectileDataBase projectile, DamageContext damageContext)
        {
            DamageContext context = SpawnEffectPayloadFactory.Create(projectile, damageContext);
            _projectileProvider.SpawnProjectile(projectile, context);
        }
    }
    internal static class SpawnEffectPayloadFactory
    {
        public static DamageContext Create(ProjectileDataBase spawnItem, DamageContext damageContext)
        {
            if (spawnItem == null)
                throw new ArgumentNullException(nameof(spawnItem));
            if (damageContext == null)
                throw new ArgumentNullException(nameof(damageContext));
            if (damageContext.Target == null)
                throw new InvalidOperationException("Projectile spawn effect requires a target.");
            if (damageContext.Attacker is not Hero)
            {
                throw new InvalidOperationException(
                    $"Projectile spawn effect requires a {nameof(Hero)} attacker. " +
                    $"Received: {damageContext.Attacker?.GetType().Name ?? "null"}.");
            }

            return damageContext.WithEffects(spawnItem.Effects);
        }
    }
}
