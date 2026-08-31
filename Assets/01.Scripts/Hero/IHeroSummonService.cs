using System;
using WhatMerge.Map;

namespace WhatMerge.Heros
{
    public interface IHeroSummonService
    {
        int SpawnedCount { get; }
        bool TrySpawnRandomHero();
        bool TrySpawnHero(int uid, int evolutionLevel);
        void SpawnHeroAtTile(int uid, int evolutionLevel, Tile tile);
        void ReturnHero(Hero hero);

        event Action<Tile, Hero> OnSpawndRanHero;
    }
}
