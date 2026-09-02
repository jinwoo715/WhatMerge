using System;
using UnityEngine;

namespace Skill
{
    public class NoneTrigger : ITrigger
    {
        public bool IsMeetTrigger(SkillTriggerContext context)
        {
            return true;
        }

        public void UseTriggerResource(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.AddHitCount(1);
        }

        public void UseTriggerResourceOnFailure(ISkillResourceModifier resourceModifier)
        {
        }
    }
    public class ManaTrigger : ITrigger, ITriggerRequirementModifier
    {
        private readonly float _baseRequiredValue;
        private float _requirementReductionRatio;
        private float _requirementReductionFixed;

        public float RequiredMana => Mathf.Max(
            1f,
            _baseRequiredValue * (1f - _requirementReductionRatio)
            - _requirementReductionFixed);

        public ManaTrigger(float requiredValue)
        {
            if (float.IsNaN(requiredValue)
                || float.IsInfinity(requiredValue)
                || requiredValue < 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredValue),
                    requiredValue,
                    "Mana requirement must be a finite number greater than or equal to one.");
            }

            _baseRequiredValue = requiredValue;
        }

        public bool IsMeetTrigger(SkillTriggerContext context)
        {
            return context.Mana >= RequiredMana;
        }

        public void UseTriggerResource(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeMana(RequiredMana);
        }

        public void UseTriggerResourceOnFailure(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeMana(RequiredMana);
        }

        public void AddRequirementReductionRatio(float ratio)
        {
            TriggerRequirementValidation.ValidateReductionRatio(ratio);
            _requirementReductionRatio = Mathf.Clamp01(_requirementReductionRatio + ratio);
        }

        public void AddRequirementReductionFixed(float value)
        {
            TriggerRequirementValidation.ValidateFixedReduction(value);
            _requirementReductionFixed += value;
        }
    }
    public class HitCountTrigger : ITrigger, ITriggerRequirementModifier
    {
        private readonly int _baseRequiredHitCount;
        private float _requirementReductionRatio;
        private float _requirementReductionFixed;

        public int RequiredHitCount
        {
            get
            {
                float reducedValue = _baseRequiredHitCount * (1f - _requirementReductionRatio)
                    - _requirementReductionFixed;
                int roundedValue = Mathf.RoundToInt(reducedValue);
                int requiredValue = Mathf.Approximately(reducedValue, roundedValue)
                    ? roundedValue
                    : Mathf.CeilToInt(reducedValue);

                return Mathf.Max(1, requiredValue);
            }
        }

        public HitCountTrigger(int requiredValue)
        {
            if (requiredValue <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredValue),
                    requiredValue,
                    "Hit count requirement must be greater than zero.");
            }

            _baseRequiredHitCount = requiredValue;
        }
        public bool IsMeetTrigger(SkillTriggerContext context)
        {
            return context.HitCount >= RequiredHitCount;
        }

        public void UseTriggerResource(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeHitCount(RequiredHitCount);
        }

        public void UseTriggerResourceOnFailure(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeHitCount(RequiredHitCount);
        }

        public void AddRequirementReductionRatio(float ratio)
        {
            TriggerRequirementValidation.ValidateReductionRatio(ratio);
            _requirementReductionRatio = Mathf.Clamp01(_requirementReductionRatio + ratio);
        }

        public void AddRequirementReductionFixed(float value)
        {
            TriggerRequirementValidation.ValidateFixedReduction(value);

            if (!Mathf.Approximately(value, Mathf.Round(value)))
            {
                throw new ArgumentException(
                    "Hit count fixed reduction must be a whole number.",
                    nameof(value));
            }

            _requirementReductionFixed += value;
        }
    }

    internal static class TriggerRequirementValidation
    {
        public static void ValidateReductionRatio(float ratio)
        {
            if (float.IsNaN(ratio)
                || float.IsInfinity(ratio)
                || ratio < 0f
                || ratio > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ratio),
                    ratio,
                    "Trigger requirement reduction ratio must be between zero and one.");
            }
        }

        public static void ValidateFixedReduction(float value)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Fixed trigger requirement reduction must be positive and finite.");
            }
        }
    }
}
