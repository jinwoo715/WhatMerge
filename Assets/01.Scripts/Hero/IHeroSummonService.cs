using System;
using Skill;
using WhatMerge.Map;

namespace WhatMerge.Heros
{
    public interface IHeroSummonService
    {
        int SpawnedCount { get; }
        bool TrySpawnRandomHero();
        bool TrySpawnHero(int uid, int evolutionLevel);
        bool CanSpawnHero(int uid);
        void SpawnHeroAtTile(int uid, int evolutionLevel, Tile tile);
        void ReturnHero(Hero hero);

        event Action<Tile, Hero> OnSpawndRanHero;
    }

    public interface IHeroSkillConfigurator
    {
        SkillController CreateSkillController(Hero hero, HeroGrade targetGrade);
    }
}
