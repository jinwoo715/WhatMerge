using Skill.Data;

namespace Skill
{
    public interface ISkillEnhancer
    {
        void ApplySkill(ISkillModifier skill);
    }
}
