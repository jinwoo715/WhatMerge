using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;
using WhatMerge.Heros;

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
        public AttackSourceSnapshot Source { get; }
        public AttackPayload AttackPayload => Source.AttackPayload;
        public int OwnerSpawnIndex => Source.OwnerSpawnIndex;
        public int SourceEvolutionLevel => Source.SourceEvolutionLevel;
        public Vector3 SourcePosition { get; }
        public CombatantTargetKind TargetKind { get; }
        public ICombatant Target;
        public Vector3 ImpactPosition;
        public List<EffectBase> Effects;
        public IRuntimeEffectLifetime EffectLifetime { get; }

        public DamageContext(
            AttackSourceSnapshot source,
            Vector3 sourcePosition,
            ICombatant target,
            List<EffectBase> effects = null,
            Vector3? impactPosition = null,
            IRuntimeEffectLifetime effectLifetime = null,
            CombatantTargetKind? targetKind = null)
        {
            Source = source;
            SourcePosition = sourcePosition;
            Target = target;
            TargetKind = targetKind ?? GetTargetKind(target);
            Effects = effects ?? new List<EffectBase>();
            EffectLifetime = effectLifetime;
            ImpactPosition = impactPosition ?? target?.Position ?? sourcePosition;
        }

        public DamageContext(DamageContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Source = context.Source;
            SourcePosition = context.SourcePosition;
            Target = context.Target;
            TargetKind = context.TargetKind;
            Effects = context.Effects ?? new List<EffectBase>();
            EffectLifetime = context.EffectLifetime;
            ImpactPosition = context.ImpactPosition;
        }

        public DamageContext WithTarget(ICombatant target)
        {
            return new DamageContext(
                Source,
                SourcePosition,
                target,
                Effects,
                effectLifetime: EffectLifetime,
                targetKind: TargetKind);
        }

        public DamageContext WithImpactPosition(Vector3 impactPosition)
        {
            return new DamageContext(
                Source,
                SourcePosition,
                null,
                Effects,
                impactPosition,
                EffectLifetime,
                TargetKind);
        }

        public DamageContext WithEffects(List<EffectBase> effects)
        {
            return new DamageContext(
                Source,
                SourcePosition,
                Target,
                effects,
                ImpactPosition,
                EffectLifetime,
                TargetKind);
        }

        public DamageContext WithSourcePosition(Vector3 sourcePosition)
        {
            return new DamageContext(
                Source,
                sourcePosition,
                Target,
                Effects,
                Target == null ? sourcePosition : ImpactPosition,
                EffectLifetime,
                TargetKind);
        }

        public DamageContext WithoutTarget()
        {
            return new DamageContext(
                Source,
                SourcePosition,
                null,
                Effects,
                ImpactPosition,
                EffectLifetime,
                TargetKind);
        }

        public IDisposable RetainEffectLifetime()
        {
            return EffectLifetime?.Retain();
        }

        private static CombatantTargetKind GetTargetKind(ICombatant target)
        {
            return target switch
            {
                null => CombatantTargetKind.None,
                Hero => CombatantTargetKind.Hero,
                Enemy => CombatantTargetKind.Enemy,
                _ => CombatantTargetKind.Other
            };
        }
    }

    public readonly struct AttackSourceSnapshot
    {
        public AttackPayload AttackPayload { get; }
        public int OwnerSpawnIndex { get; }
        public int SourceEvolutionLevel { get; }

        public AttackSourceSnapshot(
            AttackPayload attackPayload,
            int ownerSpawnIndex,
            int sourceEvolutionLevel)
        {
            AttackPayload = attackPayload;
            OwnerSpawnIndex = ownerSpawnIndex;
            SourceEvolutionLevel = sourceEvolutionLevel;
        }
    }

    public enum CombatantTargetKind
    {
        None,
        Hero,
        Enemy,
        Other
    }
}
