using Skill.Data;
using WhatMerge.Combat;

namespace Skill.Summon
{
    public interface ISummonProvider
    {
        void SpawnSummon(SummonSpawnEffect dataSO, DamageContext damageContext);
    }
}
