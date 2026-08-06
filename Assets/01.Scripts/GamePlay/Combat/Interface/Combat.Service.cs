using WhatMerge.Combat.Effects;

namespace WhatMerge.Combat
{

    public interface ICombatService
    {
        void RegisterAttack(DamageContext damageContext);
        IRuntimeEffectHandle ApplyPersistentEffects(DamageContext damageContext);
    }
}
