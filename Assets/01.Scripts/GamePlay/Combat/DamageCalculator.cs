using Skill.Data;
using System;

namespace WhatMerge.Combat
{
    public enum AttributeDamageRelation
    {
        Neutral,
        Advantage,
        Disadvantage
    }


    public interface IAttributeDamageRule
    {
        float GetMultiplier(
            ElementType attackAttribute,
            ElementType baseAttribute,
            IStatusReader temporaryAttributes);
    }

    public sealed class AttributeDamageRule : IAttributeDamageRule
    {
        private static readonly ElementType[] Attributes =
        {
            ElementType.Fire,
            ElementType.Ice,
            ElementType.Electric,
            ElementType.Earth,
            ElementType.Wind,
            ElementType.Light,
            ElementType.Dark
        };

        public static AttributeDamageRule Default { get; } = new AttributeDamageRule(1f, 1f);

        private readonly float _advantageMultiplier;
        private readonly float _disadvantageMultiplier;

        public AttributeDamageRule(float advantageMultiplier, float disadvantageMultiplier)
        {
            ValidateMultiplier(advantageMultiplier, nameof(advantageMultiplier));
            ValidateMultiplier(disadvantageMultiplier, nameof(disadvantageMultiplier));

            _advantageMultiplier = advantageMultiplier;
            _disadvantageMultiplier = disadvantageMultiplier;
        }

        public float GetMultiplier(
            ElementType attackAttribute,
            ElementType baseAttribute,
            IStatusReader temporaryAttributes)
        {
            AttributeDamageRelation relation = GetRelation(
                attackAttribute,
                baseAttribute,
                temporaryAttributes);

            return relation switch
            {
                AttributeDamageRelation.Advantage => _advantageMultiplier,
                AttributeDamageRelation.Disadvantage => _disadvantageMultiplier,
                _ => 1f
            };
        }

        public AttributeDamageRelation GetRelation(
            ElementType attackAttribute,
            ElementType baseAttribute,
            IStatusReader temporaryAttributes)
        {
            if (temporaryAttributes == null)
                throw new ArgumentNullException(nameof(temporaryAttributes));
            if (attackAttribute == ElementType.None)
                return AttributeDamageRelation.Neutral;

            bool hasAdvantage = false;
            bool hasDisadvantage = false;

            AccumulateRelation(
                GetSingleRelation(attackAttribute, baseAttribute),
                ref hasAdvantage,
                ref hasDisadvantage);

            for (int i = 0; i < Attributes.Length; i++)
            {
                ElementType temporaryAttribute = Attributes[i];

                if (!temporaryAttributes.HasStatus(temporaryAttribute))
                    continue;

                AccumulateRelation(
                    GetSingleRelation(attackAttribute, temporaryAttribute),
                    ref hasAdvantage,
                    ref hasDisadvantage);
            }

            if (hasAdvantage == hasDisadvantage)
                return AttributeDamageRelation.Neutral;

            return hasAdvantage
                ? AttributeDamageRelation.Advantage
                : AttributeDamageRelation.Disadvantage;
        }

        private static AttributeDamageRelation GetSingleRelation(
            ElementType attackAttribute,
            ElementType targetAttribute)
        {
            if (attackAttribute == ElementType.None
                || targetAttribute == ElementType.None
                || attackAttribute == targetAttribute)
            {
                return AttributeDamageRelation.Neutral;
            }

            if (IsStrongAgainst(attackAttribute, targetAttribute))
                return AttributeDamageRelation.Advantage;

            if (IsStrongAgainst(targetAttribute, attackAttribute))
                return AttributeDamageRelation.Disadvantage;

            return AttributeDamageRelation.Neutral;
        }

        private static bool IsStrongAgainst(ElementType attackAttribute, ElementType targetAttribute)
        {
            return attackAttribute switch
            {
                ElementType.Fire => targetAttribute == ElementType.Wind,
                ElementType.Ice => targetAttribute == ElementType.Fire,
                ElementType.Electric => targetAttribute == ElementType.Ice,
                ElementType.Earth => targetAttribute == ElementType.Electric,
                ElementType.Wind => targetAttribute == ElementType.Earth,
                ElementType.Light => targetAttribute == ElementType.Dark || IsFiveElement(targetAttribute),
                ElementType.Dark => targetAttribute == ElementType.Light || IsFiveElement(targetAttribute),
                _ => false
            };
        }

        private static bool IsFiveElement(ElementType attribute)
        {
            return attribute == ElementType.Fire
                || attribute == ElementType.Ice
                || attribute == ElementType.Electric
                || attribute == ElementType.Earth
                || attribute == ElementType.Wind;
        }

        private static void AccumulateRelation(
            AttributeDamageRelation relation,
            ref bool hasAdvantage,
            ref bool hasDisadvantage)
        {
            if (relation == AttributeDamageRelation.Advantage)
                hasAdvantage = true;
            else if (relation == AttributeDamageRelation.Disadvantage)
                hasDisadvantage = true;
        }

        private static void ValidateMultiplier(float multiplier, string parameterName)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    multiplier,
                    "Attribute damage multiplier must be a finite, non-negative value.");
            }
        }
    }

    public class DamageCalculator
    {
        private readonly IAttributeDamageRule _attributeDamageRule;

        public DamageCalculator(IAttributeDamageRule attributeDamageRule = null)
        {
            _attributeDamageRule = attributeDamageRule ?? AttributeDamageRule.Default;
        }

        public int CalculateFinalDamage(
            IDamageable target,
            AttackPayload payload,
            float multipleValue,
            ElementType attackAttribute = ElementType.None,
            bool ignoreArmor = false)
        {
            int armor = target.Armor;
            float reduceRatio = ignoreArmor
                ? 1f
                : 1 - StatCalculator.GetDamageReductionRate(
                    armor,
                    payload.PercentPenetration,
                    payload.FlatPenetration);
            float attributeMultiplier = _attributeDamageRule.GetMultiplier(
                attackAttribute,
                target.BaseAttribute,
                target.TemporaryAttributes);

            if (float.IsNaN(attributeMultiplier)
                || float.IsInfinity(attributeMultiplier)
                || attributeMultiplier < 0f)
            {
                throw new InvalidOperationException("Attribute damage multiplier must be a finite, non-negative value.");
            }

            bool critical = IsCritical(payload.CriticalChance);

            if (critical)
            {
                return StatCalculator.RoundInt(
                    payload.AttackDamage
                    * multipleValue
                    * payload.CriticalMultiple
                    * reduceRatio
                    * attributeMultiplier);
            }
            else
            {
                return StatCalculator.RoundInt(
                    payload.AttackDamage
                    * multipleValue
                    * reduceRatio
                    * attributeMultiplier);
            }
        }

        private bool IsCritical(int chance)
        {
            int ran = UnityEngine.Random.Range(1, 101);
            return chance >= ran;
        }

        public int CalculateDotDamage(
            IDamageable damageable,
            int calculatedDamage,
            AttackPayload payload,
            bool ignoreArmor)
        {
            if (damageable == null)
                throw new ArgumentNullException(nameof(damageable));

            if (calculatedDamage <= 0)
                return 0;

            float armorMultiplier = ignoreArmor
                ? 1f
                : 1f - StatCalculator.GetDamageReductionRate(
                    damageable.Armor,
                    payload.PercentPenetration,
                    payload.FlatPenetration);

            return StatCalculator.RoundInt(calculatedDamage * armorMultiplier);
        }
    }
}
