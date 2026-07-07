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
    }
    public class ManaTrigger : ITrigger
    {
        private float _requiredValue;
        public ManaTrigger(float requiredValue)
        {
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