using Enemies;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WhatMerge.Combat
{
    public struct AttackPayload
    {
        public readonly int AttackDamage;
        public readonly int FlatPenetration;
        public readonly float PercentPenetration;
        public readonly float CriticalChance;
        public readonly float CriticalMultiple;

        public AttackPayload(int damage, int flatPenetration, float percentPenetration, float criticalChance, float criticalMultiple)
        {
            AttackDamage = damage;
            FlatPenetration = flatPenetration;
            PercentPenetration = percentPenetration;
            CriticalChance = criticalChance;
            CriticalMultiple = criticalMultiple;
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
        public IAttacker Attacker;
        public ICombatant Target;
        public AttackPayload AttackPayload;
        public Vector3 VFXPosition;
        public List<EffectBase> skillEffects = new List<EffectBase>();

        public DamageContext(AttackPayload attackPayload, ICombatant target, IAttacker attacker)
        {
            AttackPayload = attackPayload;
            Target = target;
            Attacker = attacker;
            //VFXPosition = target.Position;
        }
    }
}
