namespace Skill
{
    public class HitCountTrigger : ISkillTriggerStrategy
    {
        private int _require;
        public void Init(float require)
        {
            _require = (int)require;
        }

        public bool CanTrigger(SkillTriggerContext context)
        {
            return _require <= context.HitCount;
        }

        public void PayCost(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeHitCount(_require);
        }
    }
}