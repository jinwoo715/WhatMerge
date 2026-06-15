namespace WhatMerge.Combat
{ 
    public interface IDamageable : ICombatant
    {
        int CurrentHP { get; }
        int Armor { get; }
        void TakeDamage(AttackResultPayload resultPayload);
    }
}
