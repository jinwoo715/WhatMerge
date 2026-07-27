using WhatMerge.Heros;

namespace WhatMerge.Combat
{
    public interface IAttacker : ICombatant
    {
        AttackPayload CreateAttackPayload();
        IHeroStatModifier StatModify { get; }
    }
}
