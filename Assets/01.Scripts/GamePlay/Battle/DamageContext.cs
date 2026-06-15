using Enemies;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WhatMerge.Combat
{
    public class AttackPayload
    {
        public int AttackDamage;
        public int FlatPenetration;
        public float PercentPenetration;
        public bool IsPiercing;

        public AttackPayload(int damage, int flatPenetration, float percentPenetration)
        {
            AttackDamage = damage;
            FlatPenetration = flatPenetration;
            PercentPenetration = percentPenetration;
            IsPiercing = false;
        }

        public void AddStatusEffect(int uid)
        {
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

    public class DamageContext
    {
        public IAttackable Attacker;
        public ICombatant Target;
        public AttackPayload AttackPayload;
        public Vector3 VFXPosition;

        public DamageContext() { }
        public DamageContext(string vfx, Vector3 vfxPosition, IAttackable attacker)
        {
            AttackPayload = new AttackPayload(0,0,0);
            Target = null;
            Attacker = attacker;
            VFXPosition = vfxPosition;
        }
        public DamageContext(AttackPayload attackPayload, ICombatant target, IAttackable attacker)
        {
            AttackPayload = attackPayload;
            Target = target;
            Attacker = attacker;
            //VFXPosition = target.Position;
        }

        public List<EffectBase> skillEffects = new List<EffectBase>();
        public void RegisterEffect(EffectBase effect)
        {
            skillEffects.Add(effect);
        }
    }
}
