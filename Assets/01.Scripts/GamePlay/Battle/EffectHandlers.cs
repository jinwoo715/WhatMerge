using Skill;
using Skill.Data;
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

    public class DamageEffectHandler : IEffectHandler
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

    public class SpawnEffectHandler : IEffectHandler
    {
        private readonly ISummonProvider _summonProvider;

        public SpawnEffectHandler(ISummonProvider summonProvider)
        {
            _summonProvider = summonProvider;
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is SpawnEffect;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (effect is not SpawnEffect spawnEffect || spawnEffect.Item is not SummonItemData summonItem)
                return;

            SkillPayload context = new SkillPayload();
            context.Target = damageContext.Target;
            context.Attacker = damageContext.Attacker as Hero;
            context.payLoad = damageContext.AttackPayload;
            context.effects = EffectRoller.GetConfirmEffects(summonItem.Effects);
            _summonProvider.SpawnSummon(summonItem, context);
        }
    }

    public class BuffEffectHandler : IEffectHandler
    {
        private readonly IBuffRegister _buffRegister;

        public BuffEffectHandler(IBuffRegister buffRegister)
        {
            _buffRegister = buffRegister;
        }

        public bool CanHandle(EffectBase effect)
        {
            return effect is BuffEffect;
        }

        public void Handle(EffectBase effect, DamageContext damageContext)
        {
            if (effect is not BuffEffect buff || damageContext.Target is not IHeroStatModifier modifier)
                return;

            _buffRegister.RegisterBuff(buff, modifier);
        }
    }
}
