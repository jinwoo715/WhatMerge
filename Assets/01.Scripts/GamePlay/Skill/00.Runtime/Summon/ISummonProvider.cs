using Skill.Data;
using WhatMerge.Combat;

namespace Skill.Summon
{
    public interface ISummonProvider
    {
        void SpawnSummon(SummonItemData dataSO, DamageContext damageContext);
    }
}
