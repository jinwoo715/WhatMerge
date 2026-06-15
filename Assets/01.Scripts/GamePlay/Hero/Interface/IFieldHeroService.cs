using Entity;
using Heros;
using Map;
using System;
using System.Collections.Generic;

public interface IFieldHeroService
{
    event Action<Hero> OnSelectHero;
    event Action OnChangedFieldHero;

    event Action<Hero> OnSpawnedHero;
    event Action<Hero> OnDestroyHero;

    IReadOnlyList<Hero> GetAllFieldHero { get; }
    void AddFieldHero(Tile tile, Hero hero);
    void SetHeroPosition(IReadOnlyTile destination, Hero hero);
    void SellHero(Hero hero);
    List<Hero> GetNearHeros(IReadOnlyTile pivot, int range);
    void SetHeroBuff(EHeroStatType stat, float value);
    void ClearHero(Hero hero);
}
