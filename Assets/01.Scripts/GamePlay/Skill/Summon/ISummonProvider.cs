using Skill.Data;

namespace Skill.Summon
{
    public interface ISummonProvider
    {
        void SpawnSummon(SummonData dataSO, SkillPayload skillPayload);
    }
}