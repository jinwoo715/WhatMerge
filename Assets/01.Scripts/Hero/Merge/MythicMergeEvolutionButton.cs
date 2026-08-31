using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Heros
{
    public class MythicMergeEvolutionButton : MonoBehaviour
    {
        private static readonly Color NormalColor = new(0.82f, 0.77f, 0.66f, 1f);
        private static readonly Color SelectedColor = new(0.13f, 0.55f, 0.5f, 1f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _mergeableIcon;

        private int _evolutionLevel;
        private bool _initialized;

        public event Action<int> OnSelected;

        public void Initialize(int evolutionLevel)
        {
            if (_initialized)
                throw new InvalidOperationException($"Evolution button '{name}' is already initialized.");
            if (_button == null || _background == null || _label == null || _mergeableIcon == null)
                throw new InvalidOperationException($"Evolution button '{name}' is not fully assigned.");
            if (evolutionLevel < 0 || evolutionLevel >= MythicMergeController.EvolutionLevelCount)
                throw new ArgumentOutOfRangeException(nameof(evolutionLevel));

            _evolutionLevel = evolutionLevel;
            _label.text = $"{evolutionLevel + 1}단계";
            _button.onClick.AddListener(HandleClick);
            _initialized = true;
            SetState(false, false);
        }

        public void SetState(bool isSelected, bool canMerge)
        {
            _background.color = isSelected ? SelectedColor : NormalColor;
            _mergeableIcon.SetActive(canMerge);
        }

        private void HandleClick()
        {
            if (_initialized)
                OnSelected?.Invoke(_evolutionLevel);
        }
    }
}
