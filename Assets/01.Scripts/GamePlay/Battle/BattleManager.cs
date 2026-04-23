using Enemies;
using Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public class BattleManager : IAttackRegister
    {
        private IVFXService _vfx;
        public event Action<Vector3, int> OnApplyDamage;

        public void Init(IVFXService vfx)
        {
            _vfx = vfx;
        }

        public void RegisterAttack(DamageContext damageContext)
        {
            _vfx.ShowEffect(damageContext.VFX, damageContext.VFXPosition, damageContext.Attacker.Position);

            if (damageContext.Target == null) return;

            int appliedDamage = CalculateFinalDamage(damageContext.Target, damageContext.AttackPayload);

            if(appliedDamage == 0) return;
            damageContext.Target.TakeDamage(new AttackResultPayload(appliedDamage));
            OnApplyDamage?.Invoke(damageContext.Target.Position, appliedDamage);
        }

        private int CalculateFinalDamage(IDamageable target, AttackPayload payload)
        {
            int amour = target.Amour;

            float reduceRatio = 1 - StatCalculator.GetDamageReductionRate(amour, payload.PercentPenetration, payload.FlatPenetration);

            return StatCalculator.RoundInt(payload.Damage * reduceRatio);
        }
    }
}