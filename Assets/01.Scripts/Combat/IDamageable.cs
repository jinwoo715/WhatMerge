using System;
using WhatMerge.Enemies;

namespace WhatMerge.Combat
{ 
    public interface IDamageable : ICombatant
    {
        int CurrentHP { get; }
        int MaxHP { get; }
        int Armor { get; }
        ElementType BaseAttribute { get; }
        IStatusReader TemporaryAttributes { get; }
        IStatusModifier TemporaryAttributeModifier { get; }
        event Action<int> OnAppliedNomalDamage;
        event Action<int, int> OnHealthChanged;
        void TakeDamage(AttackResultPayload resultPayload);
        IEnemyStatModifier StatModifier { get; }
        IMoveable Move { get; }
        void KnockBack(float distance);
    }
}
