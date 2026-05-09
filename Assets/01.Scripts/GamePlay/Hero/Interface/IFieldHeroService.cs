using Entity;
using Map;
using System;
using System.Collections.Generic;

public interface IFieldHeroService
{
    event Action<Hero> OnSelectHero;
    IReadOnlyList<Hero> GetAllFieldHero { get; }
    void AddFieldHero(Tile tile, Hero hero);
    void SetHeroPosition(IReadOnlyTile destination, Hero hero);
    void SellHero(Hero hero);
}
