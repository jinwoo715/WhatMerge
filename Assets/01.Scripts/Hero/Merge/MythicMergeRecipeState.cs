using System;
using System.Collections.Generic;

namespace WhatMerge.Heros
{
    public readonly struct MythicMergeMaterialState
    {
        public int HeroUID { get; }
        public bool IsOwned { get; }

        public MythicMergeMaterialState(int heroUID, bool isOwned)
        {
            HeroUID = heroUID;
            IsOwned = isOwned;
        }
    }

    public sealed class MythicMergeRecipeDetail
    {
        public int ResultHeroUID { get; }
        public int EvolutionLevel { get; }
        public IReadOnlyList<MythicMergeMaterialState> Materials { get; }
        public int ProgressPercent { get; }
        public bool CanMerge { get; }

        public MythicMergeRecipeDetail(
            int resultHeroUID,
            int evolutionLevel,
            IReadOnlyList<MythicMergeMaterialState> materials,
            int progressPercent,
            bool canMerge)
        {
            ResultHeroUID = resultHeroUID;
            EvolutionLevel = evolutionLevel;
            Materials = materials ?? throw new ArgumentNullException(nameof(materials));
            ProgressPercent = progressPercent;
            CanMerge = canMerge;
        }
    }

    public sealed class MythicMergeRecipeSummary
    {
        private readonly bool[] _canMergeByEvolution;

        public int ResultHeroUID { get; }
        public int ProgressPercent { get; }
        public bool CanMerge { get; }
        public int RecommendedEvolutionLevel { get; }

        public MythicMergeRecipeSummary(
            int resultHeroUID,
            int progressPercent,
            int recommendedEvolutionLevel,
            bool[] canMergeByEvolution)
        {
            if (canMergeByEvolution == null)
                throw new ArgumentNullException(nameof(canMergeByEvolution));
            if (canMergeByEvolution.Length != MythicMergeController.EvolutionLevelCount)
            {
                throw new ArgumentException(
                    $"Exactly {MythicMergeController.EvolutionLevelCount} evolution states are required.",
                    nameof(canMergeByEvolution));
            }

            ResultHeroUID = resultHeroUID;
            ProgressPercent = progressPercent;
            RecommendedEvolutionLevel = recommendedEvolutionLevel;
            _canMergeByEvolution = (bool[])canMergeByEvolution.Clone();

            bool canMerge = false;
            for (int i = 0; i < _canMergeByEvolution.Length; i++)
                canMerge |= _canMergeByEvolution[i];

            CanMerge = canMerge;
        }

        public bool CanMergeAt(int evolutionLevel)
        {
            if (evolutionLevel < 0 || evolutionLevel >= _canMergeByEvolution.Length)
                throw new ArgumentOutOfRangeException(nameof(evolutionLevel));

            return _canMergeByEvolution[evolutionLevel];
        }
    }
}
