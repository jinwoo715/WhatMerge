using Heros;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace WhatMerge.Heros
{
    public class MythicMergePanelPresenter : IDisposable
    {
        private IFieldHeroService _fieldHeroService;
        private MythicMergeController _controller;
        private MythicMergePanelViewer _viewer;
        private IHeroInfoRepository _heroRepository;
        private IResourcesReader _resourcesReader;
        private ITimeService _timeService;

        private IReadOnlyList<MythicMergeRecipeSummary> _summaries = Array.Empty<MythicMergeRecipeSummary>();
        private int _selectedResultHeroUID;
        private int _selectedEvolutionLevel;
        private bool _hasSelection;
        private bool _isOpen;
        private bool _initialized;
        private bool _disposed;

        public void Init(
            IFieldHeroService fieldHeroService,
            MythicMergeController controller,
            MythicMergePanelViewer viewer,
            IHeroInfoRepository heroRepository,
            IResourcesReader resourcesReader,
            ITimeService timeService)
        {
            if (_initialized)
                throw new InvalidOperationException("MythicMergePanelPresenter is already initialized.");

            _fieldHeroService = fieldHeroService ?? throw new ArgumentNullException(nameof(fieldHeroService));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));
            _heroRepository = heroRepository ?? throw new ArgumentNullException(nameof(heroRepository));
            _resourcesReader = resourcesReader ?? throw new ArgumentNullException(nameof(resourcesReader));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));

            _fieldHeroService.OnFieldHeroesChanged += HandleFieldHeroesChanged;
            _viewer.OnOpenRequested += Open;
            _viewer.OnCloseRequested += Close;
            _viewer.OnRecipeSelected += HandleRecipeSelected;
            _viewer.OnEvolutionSelected += HandleEvolutionSelected;
            _viewer.OnMergeRequested += HandleMergeRequested;

            _summaries = _controller.GetRecipeSummaries();
            List<MythicMergeListEntry> entries = new(_summaries.Count);

            for (int i = 0; i < _summaries.Count; i++)
            {
                MythicMergeRecipeSummary summary = _summaries[i];
                entries.Add(new MythicMergeListEntry(
                    summary.ResultHeroUID,
                    GetHeroSprite(summary.ResultHeroUID, 0)));
            }

            _viewer.InitializeRecipes(entries);
            _viewer.Hide();

            if (_summaries.Count > 0)
            {
                _selectedResultHeroUID = _summaries[0].ResultHeroUID;
                _selectedEvolutionLevel = _summaries[0].RecommendedEvolutionLevel;
                _hasSelection = true;
            }

            _initialized = true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_initialized)
            {
                _fieldHeroService.OnFieldHeroesChanged -= HandleFieldHeroesChanged;

                if (_viewer != null)
                {
                    _viewer.OnOpenRequested -= Open;
                    _viewer.OnCloseRequested -= Close;
                    _viewer.OnRecipeSelected -= HandleRecipeSelected;
                    _viewer.OnEvolutionSelected -= HandleEvolutionSelected;
                    _viewer.OnMergeRequested -= HandleMergeRequested;

                    if (_isOpen)
                        _viewer.Hide();
                }

                if (_isOpen)
                {
                    _timeService.SetPause(false);
                    _isOpen = false;
                }
            }

            _disposed = true;
        }

        private void Open()
        {
            if (!_initialized || !_hasSelection || _isOpen)
                return;

            _timeService.SetPause(true);
            _isOpen = true;
            Refresh();
            _viewer.Show();
        }

        private void Close()
        {
            if (!_isOpen)
                return;

            _viewer.Hide();
            _timeService.SetPause(false);
            _isOpen = false;
        }

        private void HandleFieldHeroesChanged()
        {
            if (_isOpen)
                Refresh();
        }

        private void HandleRecipeSelected(int resultHeroUID)
        {
            MythicMergeRecipeSummary summary = FindSummary(resultHeroUID);
            _selectedResultHeroUID = resultHeroUID;
            _selectedEvolutionLevel = summary.RecommendedEvolutionLevel;
            _hasSelection = true;
            Refresh();
        }

        private void HandleEvolutionSelected(int evolutionLevel)
        {
            if (evolutionLevel < 0 || evolutionLevel >= MythicMergeController.EvolutionLevelCount)
                return;

            _selectedEvolutionLevel = evolutionLevel;
            Refresh();
        }

        private void HandleMergeRequested()
        {
            if (!_hasSelection)
                return;

            MythicMergeCandidate candidate = new(
                _selectedResultHeroUID,
                _selectedEvolutionLevel);

            if (!_controller.TryMerge(candidate))
                Refresh();
        }

        private void Refresh()
        {
            _summaries = _controller.GetRecipeSummaries();
            if (_summaries.Count == 0)
                return;

            MythicMergeRecipeSummary selectedSummary = TryFindSummary(_selectedResultHeroUID);
            if (selectedSummary == null)
            {
                selectedSummary = _summaries[0];
                _selectedResultHeroUID = selectedSummary.ResultHeroUID;
                _selectedEvolutionLevel = selectedSummary.RecommendedEvolutionLevel;
            }

            for (int i = 0; i < _summaries.Count; i++)
            {
                MythicMergeRecipeSummary summary = _summaries[i];
                _viewer.SetRecipeState(
                    summary.ResultHeroUID,
                    summary.ProgressPercent,
                    summary.CanMerge,
                    summary.ResultHeroUID == _selectedResultHeroUID);
            }

            for (int evolutionLevel = 0;
                evolutionLevel < MythicMergeController.EvolutionLevelCount;
                evolutionLevel++)
            {
                _viewer.SetEvolutionState(
                    evolutionLevel,
                    evolutionLevel == _selectedEvolutionLevel,
                    selectedSummary.CanMergeAt(evolutionLevel));
            }

            MythicMergeRecipeDetail detail = _controller.GetRecipeDetail(
                _selectedResultHeroUID,
                _selectedEvolutionLevel);

            HeroData resultData = GetRequiredHeroData(_selectedResultHeroUID);
            if (!_heroRepository.TryGetHeroSaveData(_selectedResultHeroUID, out HeroSaveData saveData))
            {
                throw new InvalidOperationException(
                    $"Hero save data for mythic result UID {_selectedResultHeroUID} is not registered.");
            }

            _viewer.SetResult(
                resultData.Name,
                saveData.Level,
                GetHeroSprite(_selectedResultHeroUID, _selectedEvolutionLevel));

            _viewer.ClearMaterials();
            for (int i = 0; i < detail.Materials.Count; i++)
            {
                MythicMergeMaterialState material = detail.Materials[i];
                _viewer.SetMaterialState(
                    i,
                    GetHeroSprite(material.HeroUID, _selectedEvolutionLevel),
                    material.IsOwned);
            }

            _viewer.SetMergeInteractable(detail.CanMerge);
        }

        private MythicMergeRecipeSummary FindSummary(int resultHeroUID)
        {
            MythicMergeRecipeSummary summary = TryFindSummary(resultHeroUID);
            return summary
                ?? throw new InvalidOperationException(
                    $"Unlocked mythic merge recipe for hero UID {resultHeroUID} is not registered.");
        }

        private MythicMergeRecipeSummary TryFindSummary(int resultHeroUID)
        {
            for (int i = 0; i < _summaries.Count; i++)
            {
                if (_summaries[i].ResultHeroUID == resultHeroUID)
                    return _summaries[i];
            }

            return null;
        }

        private Sprite GetHeroSprite(int heroUID, int evolutionLevel)
        {
            HeroData heroData = GetRequiredHeroData(heroUID);
            SpriteAtlas atlas = _resourcesReader.GetAtlas(heroData.SpriteKey)
                ?? throw new InvalidOperationException(
                    $"Sprite atlas '{heroData.SpriteKey}' for hero UID {heroUID} is not registered.");
            string spriteName = $"{heroData.SpriteKey}_{evolutionLevel + 1}_Idle";

            return atlas.GetSprite(spriteName)
                ?? throw new InvalidOperationException(
                    $"Sprite '{spriteName}' for hero UID {heroUID} is not registered.");
        }

        private HeroData GetRequiredHeroData(int heroUID)
        {
            return _heroRepository.GetHeroData(heroUID)
                ?? throw new InvalidOperationException($"Hero data for UID {heroUID} is not registered.");
        }
    }
}
