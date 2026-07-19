using Skill;
using Skill.Data;
using Skill.Projectile;
using Skill.Summon;
using System;
using System.Collections.Generic;
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
            if (effect is not DamageEffect damageEffect || damageContext.Target is not IDamageable damageable)
                return;

            int appliedDamage = _damageCalculator.CalculateFinalDamage(damageable, damageContext.AttackPayload, damageEffect.DamageRatio);
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
                else if(spawnEffect.Execution is OnStayExecutionSummon stay)
                {
                    effects = new List<EffectBase>(stay.Effects);
                }
            }

            if (effect is SummonSpawnEffect summonSpawnEffect)
            {
                effects = EffectRoller.GetConfirmEffects(effects);

                DamageContext context = new DamageContext(damageContext.AttackPayload, damageContext.Target,
                    damageContext.Attacker, damageContext.SkillUid, damageContext.OwnerSpawnIndex, effects);

                _summonProvider.SpawnSummon(summonSpawnEffect, context);
            }
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
            return effect is ProjectileSpawnEffect projectileSpawnEffect && projectileSpawnEffect.Projectile != null
               ;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (_projectileProvider == null)
                return;

            if (effect is ProjectileSpawnEffect projectileSpawnEffect)
            {
                SpawnProjectile(projectileSpawnEffect.Projectile, damageContext);
                return;
            }
        }

        private void SpawnProjectile(ProjectileDataBase projectile, DamageContext damageContext)
        {
            if (SpawnEffectPayloadFactory.TryCreate(projectile, damageContext, out DamageContext context))
                _projectileProvider.SpawnProjectile(projectile, context);
        }
    }

    internal static class SpawnEffectPayloadFactory
    {
        public static bool TryCreate(ProjectileDataBase spawnItem, DamageContext damageContext, out DamageContext context)
        {
            context = null;

            if (spawnItem == null || damageContext == null || damageContext.Target == null || damageContext.Attacker is not Hero attacker)
                return false;

            context = new DamageContext(
                damageContext.AttackPayload,
                damageContext.Target,
                attacker,
                damageContext.SkillUid,
                damageContext.OwnerSpawnIndex,
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
