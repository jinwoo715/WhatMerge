using Skill.Data;

namespace Skill.Summon
{
    public interface ISummonProvider
    {
        void SpawnSummon(SummonItemData dataSO, SkillPayload skillPayload);
    }
}
