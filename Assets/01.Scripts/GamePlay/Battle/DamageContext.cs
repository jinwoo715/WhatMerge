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
        public IAttackable Attacker;
        public IDamageable Target;
        public AttackPayload AttackPayload;
        public string VFX;
        public Vector3 VFXPosition;

        public DamageContext(string vfx, Vector3 vfxPosition, IAttackable attacker)
        {
            AttackPayload = new AttackPayload(0,0,0);
            Target = null;
            VFX = vfx;
            Attacker = attacker;
            VFXPosition = vfxPosition;
        }

        public DamageContext(AttackPayload attackPayload, IDamageable target, string vfx, IAttackable attacker)
        {
            AttackPayload = attackPayload;
            Target = target;
            VFX = vfx;
            Attacker = attacker;
            VFXPosition = target.Position;
        }
    }
}
