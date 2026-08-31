using Heros;
using System;
using System.Collections.Generic;

namespace WhatMerge.Heros
{
    public class MythicMergeRepository
    {
        public const int MinMaterialCount = 2;
        public const int MaxMaterialCount = 4;

        private readonly List<MythicMergeData> _recipes = new();
        private readonly Dictionary<int, MythicMergeData> _recipesByResultHeroUID = new();

        public IReadOnlyList<MythicMergeData> Recipes => _recipes;

        public void Init(IReadOnlyList<MythicMergeData> recipes, IHeroInfoRepository heroRepository)
        {
            if (recipes == null)
                throw new ArgumentNullException(nameof(recipes));
            if (heroRepository == null)
                throw new ArgumentNullException(nameof(heroRepository));

            _recipes.Clear();
            _recipesByResultHeroUID.Clear();

            for (int i = 0; i < recipes.Count; i++)
            {
                MythicMergeData recipe = recipes[i]
                    ?? throw new InvalidOperationException($"Mythic merge recipe at index {i} is null.");

                ValidateRecipe(recipe, i, heroRepository);

                if (_recipesByResultHeroUID.ContainsKey(recipe.ResultHeroUID))
                {
                    throw new InvalidOperationException(
                        $"Duplicate mythic merge result hero UID: {recipe.ResultHeroUID}.");
                }

                _recipes.Add(recipe);
                _recipesByResultHeroUID.Add(recipe.ResultHeroUID, recipe);
            }
        }

        public bool TryGetRecipe(int resultHeroUID, out MythicMergeData recipe)
        {
            return _recipesByResultHeroUID.TryGetValue(resultHeroUID, out recipe);
        }

        private static void ValidateRecipe(
            MythicMergeData recipe,
            int recipeIndex,
            IHeroInfoRepository heroRepository)
        {
            ValidateHeroData(heroRepository, recipe.ResultHeroUID, $"result of recipe {recipeIndex}");

            if (recipe.Materials == null || recipe.Materials.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Mythic merge recipe for hero UID {recipe.ResultHeroUID} has no materials.");
            }

            HashSet<int> materialHeroUIDs = new();
            int totalMaterialCount = 0;

            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                MythicMergeMaterialData material = recipe.Materials[i]
                    ?? throw new InvalidOperationException(
                        $"Mythic merge recipe for hero UID {recipe.ResultHeroUID} has a null material at index {i}.");

                if (material.Count <= 0)
                {
                    throw new InvalidOperationException(
                        $"Mythic merge material hero UID {material.HeroUID} must have a positive count.");
                }

                if (!materialHeroUIDs.Add(material.HeroUID))
                {
                    throw new InvalidOperationException(
                        $"Mythic merge recipe for hero UID {recipe.ResultHeroUID} contains duplicate material UID {material.HeroUID}.");
                }

                if (material.HeroUID == recipe.ResultHeroUID)
                {
                    throw new InvalidOperationException(
                        $"Mythic merge result hero UID {recipe.ResultHeroUID} cannot be used as its own material.");
                }

                ValidateHeroData(
                    heroRepository,
                    material.HeroUID,
                    $"material {i} of recipe for hero UID {recipe.ResultHeroUID}");

                totalMaterialCount += material.Count;
            }

            if (totalMaterialCount < MinMaterialCount || totalMaterialCount > MaxMaterialCount)
            {
                throw new InvalidOperationException(
                    $"Mythic merge recipe for hero UID {recipe.ResultHeroUID} must require " +
                    $"between {MinMaterialCount} and {MaxMaterialCount} heroes.");
            }
        }

        private static void ValidateHeroData(
            IHeroInfoRepository heroRepository,
            int heroUID,
            string location)
        {
            if (heroRepository.GetHeroData(heroUID) == null)
                throw new InvalidOperationException($"Hero UID {heroUID} used as {location} is not registered.");
        }
    }
}
