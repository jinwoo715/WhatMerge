using System;

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
    }
    public class ManaTrigger : ITrigger
    {
        private readonly float _requiredValue;

        public float RequiredMana => _requiredValue;

        public ManaTrigger(float requiredValue)
        {
            if (float.IsNaN(requiredValue)
                || float.IsInfinity(requiredValue)
                || requiredValue <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredValue),
                    requiredValue,
                    "Mana requirement must be a finite number greater than zero.");
            }

            _requiredValue = requiredValue;
        }

        public bool IsMeetTrigger(SkillTriggerContext context)
        {
            return context.Mana >= _requiredValue;
        }

        public void UseTriggerResource(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeMana(_requiredValue);
        }
    }
    public class HitCountTrigger : ITrigger
    {
        private int _requireHitCount;
        public HitCountTrigger(int requiredValue)
        {
            if (requiredValue <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredValue),
                    requiredValue,
                    "Hit count requirement must be greater than zero.");
            }

            _requireHitCount = requiredValue;
        }
        public bool IsMeetTrigger(SkillTriggerContext context)
        {
            return context.HitCount >= _requireHitCount;
        }

        public void UseTriggerResource(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeHitCount(_requireHitCount);
        }
    }
}
