namespace Skill
{
    public class ManaTrigger : ISkillTriggerStrategy
    {
        private float _cost;
        public void Init(float cost)
        {
            _cost = cost;
        }
        public bool CanTrigger(SkillTriggerContext context)
        {
            return _cost <= context.Mana;
        }
        public void PayCost(ISkillResourceModifier resourceModifier)
        {
            resourceModifier.ConsumeMana(_cost);
        }
    }
}