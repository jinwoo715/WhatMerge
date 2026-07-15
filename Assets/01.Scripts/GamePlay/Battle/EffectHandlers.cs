using Skill;
using Skill.Data;
using Skill.Projectile;
using Skill.Summon;
using System;
using UnityEngine;
using WhatMerge.Heros;

namespace WhatMerge.Combat
{
    public interface IEffectHandler
    {
        bool CanHandle(EffectBase effect);
        void Handle(EffectBase effect, DamageContext damageContext);
    }

    public interface IApplyDamageNotifier
    {
        event Action<Vector3, int> OnApplyDamage;
    }

    public class DamageEffectHandler : IEffectHandler, IApplyDamageNotifier
    {
        private readonly DamageCalculator _damageCalculator;

        public event Action<Vector3, int> OnApplyDamage;

        public DamageEffectHandler(DamageCalculator damageCalculator)
        {
            _damageCalculator = damageCalculator;
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is DamageEffect;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (effect is not DamageEffect damageEffect || damageContext.Target is not IDamageable damageable)
                return;

            int appliedDamage = _damageCalculator.CalculateFinalDamage(damageable, damageContext.AttackPayload, damageEffect.DamageRatio);
            ApplyDamage(damageable, appliedDamage);
        }

        private void ApplyDamage(IDamageable damageable, int appliedDamage)
        {
            if (damageable == null || !damageable.IsActive || appliedDamage <= 0)
                return;

            damageable.TakeDamage(new AttackResultPayload(appliedDamage));
            OnApplyDamage?.Invoke(damageable.Position, appliedDamage);
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
            return effect is SpawnEffect spawnEffect && spawnEffect.Item is SummonItemData;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (_summonProvider == null || effect is not SpawnEffect spawnEffect || spawnEffect.Item is not SummonItemData summonItem)
                return;

            if (!SpawnEffectPayloadFactory.TryCreate(summonItem, damageContext, out DamageContext context))
                return;

            _summonProvider.SpawnSummon(summonItem, context);
        }
    }

    public class ProjectileSpawnEffectHandler : IEffectHandler
    {
        private readonly IProjectileProvider _projectileProvider;

        public ProjectileSpawnEffectHandler(IProjectileProvider projectileProvider)
        {
            _projectileProvider = projectileProvider;
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is SpawnEffect spawnEffect && spawnEffect.Item is ProjectileDataBase;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (_projectileProvider == null || effect is not SpawnEffect spawnEffect || spawnEffect.Item is not ProjectileDataBase projectile)
                return;

            if (!SpawnEffectPayloadFactory.TryCreate(projectile, damageContext, out DamageContext context))
                return;

            _projectileProvider.SpawnProjectile(projectile, context);
        }
    }

    internal static class SpawnEffectPayloadFactory
    {
        public static bool TryCreate(SpawnItemData spawnItem, DamageContext damageContext, out DamageContext context)
        {
            context = null;

            if (spawnItem == null || damageContext == null || damageContext.Target == null || damageContext.Attacker is not Hero attacker)
                return false;

            context = new DamageContext(
                damageContext.AttackPayload,
                damageContext.Target,
                attacker,
                EffectRoller.GetConfirmEffects(spawnItem.Effects));
            return true;
        }
    }

    public class BuffEffectHandler : IEffectHandler
    {
        private readonly IBuffService _buffRegister;

        public BuffEffectHandler(IBuffService buffRegister)
        {
            _buffRegister = buffRegister;
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is BuffEffect;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (_buffRegister == null || effect is not BuffEffect buff || damageContext.Target is not Hero hero)
                return;

            _buffRegister.EquipedBuff(buff, hero.StatModify);
        }
    }
}
