namespace WhatMerge.Combat
{
    public interface IAttacker : ICombatant
    {
        AttackPayload CreateAttackPayload();
    }
}
