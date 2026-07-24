using System;
using WhatMerge.Enemies;

namespace WhatMerge.Combat
{ 
    public interface IDamageable : ICombatant
    {
        int CurrentHP { get; }
        int MaxHP { get; }
        int Armor { get; }
        event Action<int> OnAppliedNomalDamage;
        void TakeDamage(AttackResultPayload resultPayload);
        IEnemyStatModifier StatModifier { get; }
        IMoveable Move { get; }
        void KnockBack(float distance);
    }
}
