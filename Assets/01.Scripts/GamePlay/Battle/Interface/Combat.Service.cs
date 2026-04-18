using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public interface ICreature
    {
        bool IsActive { get; }
        Vector3 Position { get; }
    }

    public interface IDamageable : ICreature
    {
        int CurrentHP { get; }
        int Amour { get; }
        EAttribute Attribute { get; }
        void TakeDamage(AttackResultPayload resultPayload);
    }

    public interface IAttackable
    {
        void RequestDamage(DamageContext dc);
        DamageContext CreateDamageContext();
    }
    public interface IAttackRegister
    {
        event Action<Vector3, int> OnApplyDamage;
        void RegisterAttack(DamageContext damageContext);
    }
} 