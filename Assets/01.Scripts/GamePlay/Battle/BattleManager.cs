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
        public event Action<Vector3, int> OnApplyDamage;

        public void RegisterAttack(DamageContext damageContext)
        {
            int appliedDamage = CalculateFinalDamage(damageContext.Target, damageContext.AttackPayload);

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