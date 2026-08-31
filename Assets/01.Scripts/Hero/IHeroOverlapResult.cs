namespace WhatMerge.Heros
{
    public interface IHeroOverlapResult
    {
        int GetMergeHeroUID(int first, int second);
        EHeroOverlapResult OverlapHero(IHeroInfoProvider first, IHeroInfoProvider second);
    }
}
