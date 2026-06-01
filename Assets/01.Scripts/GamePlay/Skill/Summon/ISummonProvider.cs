using Skill.Data;

namespace Skill.Summon
{
    public interface ISummonProvider
    {
        void SpawnSummon(SummonDataSO dataSO, SkillPayload skillPayload);
    }
}