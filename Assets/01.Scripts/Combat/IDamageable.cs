namespace WhatMerge.Combat
{ 
    public interface IDamageable : ICombatant
    {
        int CurrentHP { get; }
        int MaxHP { get; }
        int Armor { get; }
        void TakeDamage(AttackResultPayload resultPayload);
    }
}
