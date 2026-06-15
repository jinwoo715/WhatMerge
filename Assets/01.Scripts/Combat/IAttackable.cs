namespace WhatMerge.Combat
{
    public interface IAttackable
    {
        void RequestDamage(DamageContext dc);
        DamageContext CreateDamageContext();
    }
}
