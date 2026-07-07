using WhatMerge.Heros;
using WhatMerge.Map;
using System;
using System.Collections.Generic;

public interface IFieldHeroService
{
    event Action<Hero> OnSelectHero;
    event Action OnChangedHeroPosition;

    event Action<Hero> OnSpawnedHero;
    event Action<Hero> OnDestroyHero;

    IReadOnlyList<Hero> GetAllFieldHero { get; }
    void AddFieldHero(Tile tile, Hero hero);
    void SetHeroPosition(ITileReadOnly destination, Hero hero);
    void SellHero(Hero hero);
    List<Hero> GetNearHeros(ITileReadOnly pivot, int range);
    void SetHeroBuff(HeroStatType stat, float value);
    void ClearHero(Hero hero);
}
