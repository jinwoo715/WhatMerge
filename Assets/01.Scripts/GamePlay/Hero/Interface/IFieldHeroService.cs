using Entity;
using Map;
using System.Collections.Generic;

public interface IFieldHeroService
{
    int GetActiveHeroCount { get; }
    IReadOnlyList<Hero> GetAllFieldHero { get; }
    void AddFieldHero(Tile tile, Hero hero);
    void MoveHero(Tile destination, Hero hero);
}
