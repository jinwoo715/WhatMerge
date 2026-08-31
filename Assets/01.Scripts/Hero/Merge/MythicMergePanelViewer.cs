using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Heros
{
    public readonly struct MythicMergeListEntry
    {
        public int ResultHeroUID { get; }
        public Sprite HeroSprite { get; }

        public MythicMergeListEntry(int resultHeroUID, Sprite heroSprite)
        {
            ResultHeroUID = resultHeroUID;
            HeroSprite = heroSprite;
        }
    }

    public class MythicMergePanelViewer : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _panelRoot;

        [Header("Recipe List")]
        [SerializeField] private RectTransform _listContent;
        [SerializeField] private MythicMergeListItem _listItemPrefab;

        [Header("Result")]
        [SerializeField] private Image _resultHeroImage;
        [SerializeField] private TMP_Text _resultNameText;
        [SerializeField] private TMP_Text _resultLevelText;
        [SerializeField] private Button _mergeButton;

        [Header("Evolution")]
        [SerializeField] private MythicMergeEvolutionButton[] _evolutionButtons;

        [Header("Materials")]
        [SerializeField] private MythicMergeMaterialSlot[] _materialSlots;

        private readonly Dictionary<int, MythicMergeListItem> _recipeItems = new();
        private bool _recipesInitialized;

        public event Action OnOpenRequested;
        public event Action OnCloseRequested;
        public event Action OnMergeRequested;
        public event Action<int> OnRecipeSelected;
        public event Action<int> OnEvolutionSelected;

        private void Awake()
        {
            ValidateReferences();

            _openButton.onClick.AddListener(() => OnOpenRequested?.Invoke());
            _closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
            _mergeButton.onClick.AddListener(() => OnMergeRequested?.Invoke());

            for (int i = 0; i < _evolutionButtons.Length; i++)
            {
                _evolutionButtons[i].Initialize(i);
                _evolutionButtons[i].OnSelected += HandleEvolutionSelected;
            }

            for (int i = 0; i < _materialSlots.Length; i++)
            {
                _materialSlots[i].ValidateReferences();
                _materialSlots[i].Clear();
            }

            _mergeButton.interactable = false;
            _panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (_panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                OnCloseRequested?.Invoke();
        }

        public void InitializeRecipes(IReadOnlyList<MythicMergeListEntry> entries)
        {
            if (_recipesInitialized)
                throw new InvalidOperationException("Mythic merge panel recipes are already initialized.");
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            for (int i = 0; i < entries.Count; i++)
            {
                MythicMergeListEntry entry = entries[i];
                MythicMergeListItem item = Instantiate(_listItemPrefab, _listContent);
                item.Initialize(entry.ResultHeroUID, entry.HeroSprite);
                item.OnSelected += HandleRecipeSelected;
                _recipeItems.Add(entry.ResultHeroUID, item);
            }

            _recipesInitialized = true;
            _openButton.interactable = entries.Count > 0;
        }

        public void SetRecipeState(int resultHeroUID, int progressPercent, bool canMerge, bool isSelected)
        {
            if (!_recipeItems.TryGetValue(resultHeroUID, out MythicMergeListItem item))
                throw new InvalidOperationException($"Mythic merge list item for hero UID {resultHeroUID} is not initialized.");

            item.SetState(progressPercent, canMerge, isSelected);
        }

        public void SetResult(string heroName, int heroLevel, Sprite heroSprite)
        {
            if (heroSprite == null)
                throw new ArgumentNullException(nameof(heroSprite));

            _resultNameText.text = heroName;
            _resultLevelText.text = $"Lv.{heroLevel}";
            _resultHeroImage.sprite = heroSprite;
        }

        public void SetEvolutionState(int evolutionLevel, bool isSelected, bool canMerge)
        {
            if (evolutionLevel < 0 || evolutionLevel >= _evolutionButtons.Length)
                throw new ArgumentOutOfRangeException(nameof(evolutionLevel));

            _evolutionButtons[evolutionLevel].SetState(isSelected, canMerge);
        }

        public void SetMaterialState(int index, Sprite heroSprite, bool isOwned)
        {
            if (index < 0 || index >= _materialSlots.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            _materialSlots[index].SetState(heroSprite, isOwned);
        }

        public void ClearMaterials()
        {
            for (int i = 0; i < _materialSlots.Length; i++)
                _materialSlots[i].Clear();
        }

        public void SetMergeInteractable(bool interactable)
        {
            _mergeButton.interactable = interactable;
        }

        public void Show()
        {
            _panelRoot.SetActive(true);
        }

        public void Hide()
        {
            _panelRoot.SetActive(false);
        }

        private void ValidateReferences()
        {
            if (_openButton == null
                || _closeButton == null
                || _panelRoot == null
                || _listContent == null
                || _listItemPrefab == null
                || _resultHeroImage == null
                || _resultNameText == null
                || _resultLevelText == null
                || _mergeButton == null)
            {
                throw new InvalidOperationException($"Mythic merge panel viewer '{name}' is not fully assigned.");
            }

            if (_evolutionButtons == null
                || _evolutionButtons.Length != MythicMergeController.EvolutionLevelCount)
            {
                throw new InvalidOperationException(
                    $"Mythic merge panel requires exactly {MythicMergeController.EvolutionLevelCount} evolution buttons.");
            }

            if (_materialSlots == null
                || _materialSlots.Length != MythicMergeRepository.MaxMaterialCount)
            {
                throw new InvalidOperationException(
                    $"Mythic merge panel requires exactly {MythicMergeRepository.MaxMaterialCount} material slots.");
            }

            for (int i = 0; i < _evolutionButtons.Length; i++)
            {
                if (_evolutionButtons[i] == null)
                    throw new InvalidOperationException($"Evolution button {i} is not assigned.");
            }

            for (int i = 0; i < _materialSlots.Length; i++)
            {
                if (_materialSlots[i] == null)
                    throw new InvalidOperationException($"Material slot {i} is not assigned.");
            }
        }

        private void HandleRecipeSelected(int resultHeroUID)
        {
            OnRecipeSelected?.Invoke(resultHeroUID);
        }

        private void HandleEvolutionSelected(int evolutionLevel)
        {
            OnEvolutionSelected?.Invoke(evolutionLevel);
        }
    }
}
