using Skill.Data;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Combat
{
    public struct AttackPayload
    {
        public readonly int AttackDamage;
        public readonly int FlatPenetration;
        public readonly float PercentPenetration;
        public readonly int CriticalChance;
        public readonly float CriticalMultiple;

        public AttackPayload(int damage, int flatPenetration, float percentPenetration, int criticalChance, float criticalMultiple)
        {
            AttackDamage = damage;
            FlatPenetration = flatPenetration;
            PercentPenetration = percentPenetration;
            CriticalChance = criticalChance;
            CriticalMultiple = criticalMultiple;
        }
    }


    public enum DamageResultType
    {
        NomalDamage,
        TransferDamage,
        ExecutionDamage
    }

    public struct AttackResultPayload
    {
        public int Damage;
        public DamageResultType ResultType;
        public AttackResultPayload(int damage, DamageResultType type = DamageResultType.NomalDamage)
        {
            Damage = damage;
            ResultType = type;
        }
    }

    public class DamageContext
    {
        public IAttacker Attacker;
        public ICombatant Target;
        public AttackPayload AttackPayload;
        public Vector3 VFXPosition;
        public List<EffectBase> Effects;
        public int SkillUid;
        public int OwnerSpawnIndex;

        public DamageContext(AttackPayload attackPayload, ICombatant target, IAttacker attacker, int skillUid, int ownerSpawnIndex, List<EffectBase> effects = null)
        {
            AttackPayload = attackPayload;
            Target = target;
            Attacker = attacker;
            Effects = effects ?? new List<EffectBase>();
            SkillUid = skillUid;
            OwnerSpawnIndex = ownerSpawnIndex;
            //VFXPosition = target.Position;
        }

        public DamageContext WithTarget(ICombatant target)
        {
            return new DamageContext(AttackPayload, target, Attacker, SkillUid, OwnerSpawnIndex, Effects);
        }
    }
}
