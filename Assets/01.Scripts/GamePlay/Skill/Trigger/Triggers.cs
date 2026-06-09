using UnityEngine;

namespace Skill
{
    public class NoneRequire : ITrigger
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
    public class ManaRequire : ITrigger
    {
        private float _requiredValue;
        public ManaRequire(float requiredValue)
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
    public class HitCountRequire : ITrigger
    {
        private int _requireHitCount;
        public HitCountRequire(int requiredValue)
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