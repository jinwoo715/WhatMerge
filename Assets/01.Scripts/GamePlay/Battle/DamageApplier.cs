using System;
using UnityEngine;

namespace WhatMerge.Combat
{
    public interface IDamageApplier : IApplyDamageNotifier
    {
        bool TryApply(IDamageable target, int damage, DamageResultType type = DamageResultType.NomalDamage);
    }

    public sealed class DamageApplier : IDamageApplier
    {
        public event Action<Vector3, int> OnApplyDamage;

        public bool TryApply(IDamageable target, int damage, DamageResultType type = DamageResultType.NomalDamage)
        {
            if (target == null || !target.IsActive || damage <= 0)
                return false;

            Vector3 position = target.Position;
            target.TakeDamage(new AttackResultPayload(damage, type));
            OnApplyDamage?.Invoke(position, damage);
            return true;
        }
    }
}
