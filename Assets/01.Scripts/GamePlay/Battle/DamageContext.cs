using Enemies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Combat
{
    public struct AttackPayload
    {
        public int Damage;
        public int FlatPenetration;
        public float PercentPenetration;

        public AttackPayload(int damage, int flatPenetration, float percentPenetration)
        {
            Damage = damage;
            FlatPenetration = flatPenetration;
            PercentPenetration = percentPenetration;
        }
    }

    public struct AttackResultPayload
    {
        public int Damage;

        public AttackResultPayload(int damage)
        {
            Damage = damage;
        }
    }

    public struct DamageContext
    {
        public IDamageable Target;
        public AttackPayload AttackPayload;

        public DamageContext(AttackPayload attackPayload, IDamageable target)
        {
            AttackPayload = attackPayload;
            Target = target;
        }
    }
}
