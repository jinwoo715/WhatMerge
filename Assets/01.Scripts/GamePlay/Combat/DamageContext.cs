using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Combat
{
    public interface IRuntimeEffectLifetime
    {
        IDisposable Retain();
    }

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
        public Vector3 ImpactPosition;
        public List<EffectBase> Effects;
        public int SkillUid;
        public int OwnerSpawnIndex;
        public IRuntimeEffectLifetime EffectLifetime { get; }

        public DamageContext(
            AttackPayload attackPayload,
            ICombatant target,
            IAttacker attacker,
            int skillUid,
            int ownerSpawnIndex,
            List<EffectBase> effects = null,
            Vector3? impactPosition = null,
            IRuntimeEffectLifetime effectLifetime = null)
        {
            AttackPayload = attackPayload;
            Target = target;
            Attacker = attacker;
            Effects = effects ?? new List<EffectBase>();
            SkillUid = skillUid;
            OwnerSpawnIndex = ownerSpawnIndex;
            EffectLifetime = effectLifetime;
            ImpactPosition = impactPosition
                ?? target?.Position
                ?? attacker?.Position
                ?? Vector3.zero;
        }

        public DamageContext(DamageContext context)
        {
            AttackPayload = context.AttackPayload;
            Target = context.Target;
            Attacker = context.Attacker;
            Effects = context.Effects ?? new List<EffectBase>();
            SkillUid = context.SkillUid;
            OwnerSpawnIndex = context.OwnerSpawnIndex;
            EffectLifetime = context.EffectLifetime;
            ImpactPosition = context.ImpactPosition;
        }

        public DamageContext WithTarget(ICombatant target)
        {
            return new DamageContext(
                AttackPayload,
                target,
                Attacker,
                SkillUid,
                OwnerSpawnIndex,
                Effects,
                effectLifetime: EffectLifetime);
        }

        public DamageContext WithImpactPosition(Vector3 impactPosition)
        {
            return new DamageContext(
                AttackPayload,
                null,
                Attacker,
                SkillUid,
                OwnerSpawnIndex,
                Effects,
                impactPosition,
                EffectLifetime);
        }

        public DamageContext WithEffects(List<EffectBase> effects)
        {
            return new DamageContext(
                AttackPayload,
                Target,
                Attacker,
                SkillUid,
                OwnerSpawnIndex,
                effects,
                ImpactPosition,
                EffectLifetime);
        }

        public IDisposable RetainEffectLifetime()
        {
            return EffectLifetime?.Retain();
        }
    }
}
