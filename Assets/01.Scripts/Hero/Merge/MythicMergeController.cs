using Heros;
using System;
using System.Collections.Generic;

namespace WhatMerge.Heros
{
    public class MythicMergeController
    {
        public const int MaxVisibleCandidateCount = 3;
        public const int EvolutionLevelCount = 3;

        private MythicMergeRepository _repository;
        private IFieldHeroService _fieldHeroService;
        private IHeroMergeExecutor _mergeExecutor;
        private IHeroInfoRepository _heroRepository;

        public void Init(
            MythicMergeRepository repository,
            IFieldHeroService fieldHeroService,
            IHeroMergeExecutor mergeExecutor,
            IHeroInfoRepository heroRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _fieldHeroService = fieldHeroService ?? throw new ArgumentNullException(nameof(fieldHeroService));
            _mergeExecutor = mergeExecutor ?? throw new ArgumentNullException(nameof(mergeExecutor));
            _heroRepository = heroRepository ?? throw new ArgumentNullException(nameof(heroRepository));
        }

        public IReadOnlyList<MythicMergeCandidate> GetAvailableCandidates()
        {
            EnsureInitialized();

            Dictionary<int, Dictionary<int, int>> countsByEvolution =
                BuildHeroCounts(_fieldHeroService.GetAllFieldHero);
            List<MythicMergeCandidate> candidates = new(MaxVisibleCandidateCount);

            IReadOnlyList<MythicMergeData> recipes = _repository.Recipes;
            for (int recipeIndex = 0; recipeIndex < recipes.Count; recipeIndex++)
            {
                MythicMergeData recipe = recipes[recipeIndex];
                if (!IsUnlocked(recipe.ResultHeroUID))
                    continue;

                for (int evolutionLevel = 0; evolutionLevel < EvolutionLevelCount; evolutionLevel++)
                {
                    if (!HasAllMaterials(recipe, countsByEvolution[evolutionLevel]))
                        continue;

                    candidates.Add(new MythicMergeCandidate(recipe.ResultHeroUID, evolutionLevel));
                    if (candidates.Count == MaxVisibleCandidateCount)
                        return candidates;
                }
            }

            return candidates;
        }

        public IReadOnlyList<MythicMergeRecipeSummary> GetRecipeSummaries()
        {
            EnsureInitialized();

            Dictionary<int, Dictionary<int, int>> countsByEvolution =
                BuildHeroCounts(_fieldHeroService.GetAllFieldHero);
            List<MythicMergeRecipeSummary> summaries = new();

            IReadOnlyList<MythicMergeData> recipes = _repository.Recipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                MythicMergeData recipe = recipes[i];
                if (!IsUnlocked(recipe.ResultHeroUID))
                    continue;

                summaries.Add(BuildSummary(recipe, countsByEvolution));
            }

            return summaries;
        }

        public MythicMergeRecipeDetail GetRecipeDetail(int resultHeroUID, int evolutionLevel)
        {
            EnsureInitialized();
            ValidateEvolutionLevel(evolutionLevel);

            if (!IsUnlocked(resultHeroUID))
                throw new InvalidOperationException($"Mythic result hero UID {resultHeroUID} is not unlocked.");
            if (!_repository.TryGetRecipe(resultHeroUID, out MythicMergeData recipe))
                throw new InvalidOperationException($"Mythic merge recipe for hero UID {resultHeroUID} is not registered.");

            Dictionary<int, Dictionary<int, int>> countsByEvolution =
                BuildHeroCounts(_fieldHeroService.GetAllFieldHero);
            return BuildDetail(recipe, evolutionLevel, countsByEvolution[evolutionLevel]);
        }

        public bool TryMerge(MythicMergeCandidate candidate)
        {
            EnsureInitialized();

            if (candidate.EvolutionLevel < 0 || candidate.EvolutionLevel >= EvolutionLevelCount)
                return false;
            if (!IsUnlocked(candidate.ResultHeroUID))
                return false;
            if (!_repository.TryGetRecipe(candidate.ResultHeroUID, out MythicMergeData recipe))
                return false;

            List<Hero> materials = SelectMaterials(recipe, candidate.EvolutionLevel);
            if (materials == null)
                return false;

            return _mergeExecutor.TryMergeHeroes(materials, candidate.ResultHeroUID, candidate.EvolutionLevel);
        }

        private MythicMergeRecipeSummary BuildSummary(MythicMergeData recipe, IReadOnlyDictionary<int, Dictionary<int, int>> countsByEvolution)
        {
            bool[] canMergeByEvolution = new bool[EvolutionLevelCount];
            int recommendedEvolutionLevel = 0;
            int bestProgress = -1;
            int lowestCompleteEvolutionLevel = -1;

            for (int evolutionLevel = 0; evolutionLevel < EvolutionLevelCount; evolutionLevel++)
            {
                MythicMergeRecipeDetail detail = BuildDetail(
                    recipe,
                    evolutionLevel,
                    countsByEvolution[evolutionLevel]);

                canMergeByEvolution[evolutionLevel] = detail.CanMerge;
                if (detail.CanMerge && lowestCompleteEvolutionLevel < 0)
                    lowestCompleteEvolutionLevel = evolutionLevel;

                if (detail.ProgressPercent > bestProgress)
                {
                    bestProgress = detail.ProgressPercent;
                    recommendedEvolutionLevel = evolutionLevel;
                }
            }

            if (lowestCompleteEvolutionLevel >= 0)
                recommendedEvolutionLevel = lowestCompleteEvolutionLevel;

            return new MythicMergeRecipeSummary(
                recipe.ResultHeroUID,
                bestProgress,
                recommendedEvolutionLevel,
                canMergeByEvolution);
        }

        private static MythicMergeRecipeDetail BuildDetail(
            MythicMergeData recipe,
            int evolutionLevel,
            IReadOnlyDictionary<int, int> heroCounts)
        {
            List<MythicMergeMaterialState> materialStates = new(MythicMergeRepository.MaxMaterialCount);
            int ownedSlotCount = 0;

            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                MythicMergeMaterialData material = recipe.Materials[i];
                heroCounts.TryGetValue(material.HeroUID, out int ownedCount);

                for (int countIndex = 0; countIndex < material.Count; countIndex++)
                {
                    bool isOwned = countIndex < ownedCount;
                    materialStates.Add(new MythicMergeMaterialState(material.HeroUID, isOwned));
                    if (isOwned)
                        ownedSlotCount++;
                }
            }

            int progressPercent = ownedSlotCount * 100 / materialStates.Count;
            bool canMerge = ownedSlotCount == materialStates.Count;

            return new MythicMergeRecipeDetail(
                recipe.ResultHeroUID,
                evolutionLevel,
                materialStates,
                progressPercent,
                canMerge);
        }

        private List<Hero> SelectMaterials(MythicMergeData recipe, int evolutionLevel)
        {
            IReadOnlyList<Hero> fieldHeroes = _fieldHeroService.GetAllFieldHero;
            Dictionary<int, List<Hero>> heroesByUID = new();

            for (int i = 0; i < fieldHeroes.Count; i++)
            {
                Hero hero = fieldHeroes[i];
                if (hero.EvolutionLevel != evolutionLevel)
                    continue;

                if (!heroesByUID.TryGetValue(hero.UID, out List<Hero> heroes))
                {
                    heroes = new List<Hero>();
                    heroesByUID.Add(hero.UID, heroes);
                }

                heroes.Add(hero);
            }

            foreach (List<Hero> heroes in heroesByUID.Values)
                heroes.Sort((left, right) => left.SpawnIndex.CompareTo(right.SpawnIndex));

            List<Hero> selectedMaterials = new();
            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                MythicMergeMaterialData material = recipe.Materials[i];
                if (!heroesByUID.TryGetValue(material.HeroUID, out List<Hero> heroes)
                    || heroes.Count < material.Count)
                {
                    return null;
                }

                for (int count = 0; count < material.Count; count++)
                    selectedMaterials.Add(heroes[count]);
            }

            selectedMaterials.Sort((left, right) => left.SpawnIndex.CompareTo(right.SpawnIndex));
            return selectedMaterials;
        }

        private static Dictionary<int, Dictionary<int, int>> BuildHeroCounts(
            IReadOnlyList<Hero> fieldHeroes)
        {
            Dictionary<int, Dictionary<int, int>> countsByEvolution = new(EvolutionLevelCount);
            for (int evolutionLevel = 0; evolutionLevel < EvolutionLevelCount; evolutionLevel++)
                countsByEvolution.Add(evolutionLevel, new Dictionary<int, int>());

            for (int i = 0; i < fieldHeroes.Count; i++)
            {
                Hero hero = fieldHeroes[i];
                if (hero.EvolutionLevel < 0 || hero.EvolutionLevel >= EvolutionLevelCount)
                    continue;

                Dictionary<int, int> countsByUID = countsByEvolution[hero.EvolutionLevel];
                countsByUID.TryGetValue(hero.UID, out int count);
                countsByUID[hero.UID] = count + 1;
            }

            return countsByEvolution;
        }

        private static bool HasAllMaterials(
            MythicMergeData recipe,
            IReadOnlyDictionary<int, int> heroCounts)
        {
            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                MythicMergeMaterialData material = recipe.Materials[i];
                if (!heroCounts.TryGetValue(material.HeroUID, out int count)
                    || count < material.Count)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsUnlocked(int resultHeroUID)
        {
            return _heroRepository.TryGetHeroSaveData(resultHeroUID, out _);
        }

        private static void ValidateEvolutionLevel(int evolutionLevel)
        {
            if (evolutionLevel < 0 || evolutionLevel >= EvolutionLevelCount)
                throw new ArgumentOutOfRangeException(nameof(evolutionLevel));
        }

        private void EnsureInitialized()
        {
            if (_repository == null
                || _fieldHeroService == null
                || _mergeExecutor == null
                || _heroRepository == null)
            {
                throw new InvalidOperationException("MythicMergeController is not initialized.");
            }
        }
    }
}
