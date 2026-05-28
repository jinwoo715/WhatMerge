using Enemies;
using Entity;
using Skill;
using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public class BattleManager : ICombatService
    {
        private ISummonProvider _summonProvider;
        private IVFXService _vfx;
        public event Action<Vector3, int> OnApplyDamage;

        public void Init(IVFXService vfx, ISummonProvider summonProvider)
        {
            _vfx = vfx;
            _summonProvider = summonProvider;
        }

        public void RegisterAttack(DamageContext damageContext)
        {
            //_vfx.ShowEffect(damageContext.VFX, damageContext.VFXPosition, damageContext.Attacker.Position);
            //Debug.Log(damageContext.AttackPayload.Damage);

            //if (damageContext.Target == null) return;

            //int appliedDamage = CalculateFinalDamage(damageContext.Target, damageContext.AttackPayload);

            //if(appliedDamage == 0) return;
            //damageContext.Target.TakeDamage(new AttackResultPayload(appliedDamage));
            //OnApplyDamage?.Invoke(damageContext.Target.Position, appliedDamage);

            Debug.Log($"들어왔다! {damageContext.skillEffects.Count}");
            ProcessEffects(damageContext.skillEffects, damageContext);
        }

        private int CalculateFinalDamage(IDamageable target, AttackPayload payload)
        {
            int amour = target.Amour;

            float reduceRatio = 1 - StatCalculator.GetDamageReductionRate(amour, payload.PercentPenetration, payload.FlatPenetration);

            return StatCalculator.RoundInt(payload.Damage * reduceRatio);
        }

        private void ProcessEffects(List<EffectBase> effects, DamageContext damageContext)
        {
            Debug.Log($"여기도! {effects.Count}");
            foreach (var effect in effects)
            {
                if (effect is SummonEffect summon)
                {
                    Debug.Log("소환!!!!!");
                    ProjectileEventContext context = new ProjectileEventContext();
                    context.Target = damageContext.Target;
                    context.Attacker = damageContext.Attacker as Hero;
                    context.effects = effects;
                    context.payLoad = damageContext.AttackPayload;
                    _summonProvider.SpawnSummon(summon.Summon, context);
                }
            }
        }
    }
}