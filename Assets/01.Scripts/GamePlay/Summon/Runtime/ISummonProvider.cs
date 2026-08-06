using Skill.Data;
using WhatMerge.Combat;

namespace WhatMerge.Summons
{
    public interface ISummonProvider
    {
        void SpawnSummon(SummonSpawnEffect dataSO, DamageContext damageContext);
    }
}
