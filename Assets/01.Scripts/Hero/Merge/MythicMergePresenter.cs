using Heros;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace WhatMerge.Heros
{
    public class MythicMergePresenter
    {
        private IFieldHeroService _fieldHeroService;
        private MythicMergeController _controller;
        private MythicMergeViewer _viewer;
        private IHeroInfoRepository _heroRepository;
        private IResourcesReader _resourcesReader;

        public void Init(
            IFieldHeroService fieldHeroService,
            MythicMergeController controller,
            MythicMergeViewer viewer,
            IHeroInfoRepository heroRepository,
            IResourcesReader resourcesReader)
        {
            _fieldHeroService = fieldHeroService ?? throw new ArgumentNullException(nameof(fieldHeroService));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));
            _heroRepository = heroRepository ?? throw new ArgumentNullException(nameof(heroRepository));
            _resourcesReader = resourcesReader ?? throw new ArgumentNullException(nameof(resourcesReader));

            _fieldHeroService.OnFieldHeroesChanged += Refresh;
            _viewer.OnMergeRequested += HandleMergeRequested;

            Refresh();
        }

        private void HandleMergeRequested(MythicMergeCandidate candidate)
        {
            if (!_controller.TryMerge(candidate))
                Refresh();
        }

        private void Refresh()
        {
            IReadOnlyList<MythicMergeCandidate> candidates = _controller.GetAvailableCandidates();
            _viewer.Clear();

            for (int i = 0; i < candidates.Count; i++)
            {
                MythicMergeCandidate candidate = candidates[i];
                Sprite sprite = GetHeroSprite(candidate);
                _viewer.SetCandidate(i, candidate, sprite);
            }
        }

        private Sprite GetHeroSprite(MythicMergeCandidate candidate)
        {
            HeroData heroData = _heroRepository.GetHeroData(candidate.ResultHeroUID)
                ?? throw new InvalidOperationException(
                    $"Hero data for mythic merge result UID {candidate.ResultHeroUID} is not registered.");
            SpriteAtlas atlas = _resourcesReader.GetAtlas(heroData.SpriteKey)
                ?? throw new InvalidOperationException(
                    $"Sprite atlas '{heroData.SpriteKey}' for mythic merge result UID {candidate.ResultHeroUID} is not registered.");
            string spriteName = $"{heroData.SpriteKey}_{candidate.EvolutionLevel + 1}_Idle";

            return atlas.GetSprite(spriteName)
                ?? throw new InvalidOperationException(
                    $"Sprite '{spriteName}' for mythic merge result UID {candidate.ResultHeroUID} is not registered.");
        }
    }
}
