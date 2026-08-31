using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Heros
{
    public class MythicMergeListItem : MonoBehaviour
    {
        private static readonly Color NormalColor = new(0.32f, 0.29f, 0.3f, 1f);
        private static readonly Color SelectedColor = new(0.85f, 0.7f, 0.15f, 1f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _heroImage;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _mergeableIcon;

        private int _resultHeroUID;
        private bool _initialized;

        public event Action<int> OnSelected;

        private void Awake()
        {
            if (_button == null
                || _background == null
                || _heroImage == null
                || _progressText == null
                || _mergeableIcon == null)
            {
                throw new InvalidOperationException($"Mythic merge list item '{name}' is not fully assigned.");
            }

            _button.onClick.AddListener(HandleClick);
        }

        public void Initialize(int resultHeroUID, Sprite heroSprite)
        {
            if (_initialized)
                throw new InvalidOperationException($"Mythic merge list item '{name}' is already initialized.");
            if (heroSprite == null)
                throw new ArgumentNullException(nameof(heroSprite));

            _resultHeroUID = resultHeroUID;
            _heroImage.sprite = heroSprite;
            _initialized = true;
            SetState(0, false, false);
        }

        public void SetState(int progressPercent, bool canMerge, bool isSelected)
        {
            if (!_initialized)
                return;

            _progressText.gameObject.SetActive(!canMerge);
            _progressText.text = $"{progressPercent}%";
            _mergeableIcon.SetActive(canMerge);
            _background.color = isSelected ? SelectedColor : NormalColor;
        }

        private void HandleClick()
        {
            if (_initialized)
                OnSelected?.Invoke(_resultHeroUID);
        }
    }
}
