using System.Collections.Generic;

namespace WhatMerge.Heros
{
    public interface IHeroMergeExecutor
    {
        bool TryMergeHeroes(
            IReadOnlyList<Hero> materials,
            int resultHeroUID,
            int evolutionLevel);
    }
}
