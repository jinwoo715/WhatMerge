namespace Skill
{
    public class NoneTrigger : ISkillTriggerStrategy
    {
        public void Init(float cost) { }
        public bool CanTrigger(SkillTriggerContext context) => true;
        public void PayCost(ISkillResourceModifier resourceModifier) { }
    }
}