using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public interface ICreature : WhatMerge.Combat.ICombatant
    {
    }

namespace WhatMerge.Combat
{

    public interface ICombatService
    {
        event Action<Vector3, int> OnApplyDamage;
        void RegisterAttack(DamageContext damageContext);
    }
}
