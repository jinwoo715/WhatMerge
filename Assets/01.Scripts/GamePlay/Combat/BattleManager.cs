using Skill;
using Skill.Data;
using Skill.Summon;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Heros;

namespace WhatMerge.Combat
{
    public class DamageCalculator
    {
        //public AttackResultPayload Calculate()
        //{

        //}
    }

    public class BattleManager : ICombatService
    {
        private IBuffRegister _buffRegister;
        private ISummonProvider _summonProvider;
        private IVFXService _vfx;
        public event Action<Vector3, int> OnApplyDamage;

        public void Init(IVFXService vfx, ISummonProvider summonProvider, IBuffRegister buffRegister)
        {
            _vfx = vfx;
            _summonProvider = summonProvider;
            _buffRegister = buffRegister;
        }

        public void RegisterAttack(DamageContext damageContext)
        {
            ProcessEffects(damageContext.skillEffects, damageContext);
        }

        private void ProcessEffects(List<EffectBase> effects, DamageContext damageContext)
        {
            foreach (var effect in effects)
            {
                if (effect is SpawnEffect spawn)
                {
                    ProcessSpawnEffect(spawn, damageContext);
                }
                else if (effect is DamageEffect damage)
                {
                    ProcessDamageEffect(damageContext, damage);
                }
                else if(effect is BuffEffect buff)
                {
                    Debug.Log(damageContext.Target);
                    ProcessBuffEffect(damageContext.Target, buff);
                }
            }
        }

        private void ProcessSpawnEffect(SpawnEffect spawnEffect, DamageContext damageContext)
        {
            if (!(spawnEffect.Item is SummonItemData summonItem))
            {
                return;
            }

            SkillPayload context = new SkillPayload();
            context.Target = damageContext.Target;
            context.Attacker = damageContext.Attacker as Hero;
            context.payLoad = damageContext.AttackPayload;
            context.effects = EffectRoller.GetConfirmEffects(summonItem.Effects);
            _summonProvider.SpawnSummon(summonItem, context);
        }

        private void ProcessDamageEffect(DamageContext damageContext, DamageEffect effect)
        {
            //_vfx.ShowEffect(damageContext.VFX, damageContext.VFXPosition, damageContext.Attacker.Position);
            //Debug.Log(damageContext.AttackPayload.Damage);

            if (damageContext.Target == null) return;

            IDamageable damageable = damageContext.Target as IDamageable;

            int appliedDamage = CalculateFinalDamage(damageable, damageContext.AttackPayload, effect.DamageRatio);

            if(appliedDamage == 0) return;

            damageable.TakeDamage(new AttackResultPayload(appliedDamage));
            OnApplyDamage?.Invoke(damageContext.Target.Position, appliedDamage);
        }

        //private int GetTotalDamage()
        //{

        //}
        
        private int CalculateFinalDamage(IDamageable target, AttackPayload payload, float multipleValue)
        {
            int amour = target.Armor;

            float reduceRatio = 1 - StatCalculator.GetDamageReductionRate(amour, payload.PercentPenetration, payload.FlatPenetration);

            return StatCalculator.RoundInt(payload.AttackDamage * multipleValue * reduceRatio);
        }

        private void ProcessBuffEffect(ICombatant target, BuffEffect buff)
        {
            Debug.Log($"Àû¿ë : {target}");

            if (target is IHeroStatModifier modifier)
            {
                _buffRegister.RegisterBuff(buff, modifier);
            }
            else
            {
                Debug.Log($"¾Æ´Ô : {target}");
            }
        }
    }
}
