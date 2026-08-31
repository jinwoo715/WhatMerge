using UnityEngine;

namespace WhatMerge.Heros
{
    public class HeroOverlapProcessor : IHeroOverlapResult
    {
        private NomalMergeRepository _mergeRepository;

        public void Init(NomalMergeRepository mergeRepository)
        {
            _mergeRepository = mergeRepository;
        }

        public int GetMergeHeroUID(int first, int second)
        {
            return _mergeRepository.GetMergeResult(first, second);
        }

        public EHeroOverlapResult OverlapHero(IHeroInfoProvider first, IHeroInfoProvider second)
        {
            Debug.Log($"{first.EvolutionLevel} / {second.EvolutionLevel}");
            if (first.EvolutionLevel != second.EvolutionLevel)
                return EHeroOverlapResult.None;

            Debug.Log($"{first.UID == second.UID && first.EvolutionLevel < 2}");
            if (first.UID == second.UID && first.EvolutionLevel < 2)
                return EHeroOverlapResult.Evolution;

            if (_mergeRepository.IsCanMerge(first.UID, second.UID))
            {
                return EHeroOverlapResult.Merge;
            }

            return EHeroOverlapResult.None;
        }
    }
}
