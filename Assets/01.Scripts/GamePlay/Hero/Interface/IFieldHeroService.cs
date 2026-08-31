using WhatMerge.Heros;
using WhatMerge.Map;
using System;
using System.Collections.Generic;
using Skill.Data;

public interface IFieldHeroService
{
    event Action<Hero> OnSelectHero;
    event Action OnChangedHeroPosition;
    event Action OnFieldHeroesChanged;

    event Action<Hero> OnSpawnedHero;
    event Action<Hero> OnDestroyHero;

    IReadOnlyList<Hero> GetAllFieldHero { get; }
    void AddFieldHero(Tile tile, Hero hero);
    void SetHeroPosition(ITileReadOnly destination, Hero hero);
    void SellHero(Hero hero);
    List<Hero> GetNearHeros(ITileReadOnly pivot, HeroSearchType range);
    void ClearHero(Hero hero);
}


