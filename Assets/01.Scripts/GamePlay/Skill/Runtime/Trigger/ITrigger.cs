namespace Skill
{
    public interface ITrigger
    {
        bool IsMeetTrigger(SkillTriggerContext context);
        void UseTriggerResource(ISkillResourceModifier resourceModifier);
        void UseTriggerResourceOnFailure(ISkillResourceModifier resourceModifier);
    }

    public interface ITriggerRequirementModifier
    {
        void AddRequirementReductionRatio(float ratio);
    }
}
