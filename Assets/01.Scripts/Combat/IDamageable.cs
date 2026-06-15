namespace WhatMerge.Combat
{ 
    public interface IDamageable : ICombatant
    {
        int CurrentHP { get; }
        int Armor { get; }
        ElementType Element { get; }
        void SetAttribute(ElementType attributeType, float duration);
        void TakeDamage(AttackResultPayload resultPayload);
    }
}
