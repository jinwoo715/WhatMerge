using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Heros
{
    public class MythicMergeButtonSlot : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _heroImage;
        [SerializeField] private TMP_Text _evolutionText;

        private MythicMergeCandidate _candidate;
        private bool _hasCandidate;

        public event Action<MythicMergeCandidate> OnClick;

        private void Awake()
        {
            if (_button == null || _heroImage == null || _evolutionText == null)
                throw new InvalidOperationException($"Mythic merge button slot '{name}' is not fully assigned.");

            _button.onClick.AddListener(HandleClick);
        }

        public void SetCandidate(MythicMergeCandidate candidate, Sprite heroSprite)
        {
            if (heroSprite == null)
                throw new ArgumentNullException(nameof(heroSprite));

            _candidate = candidate;
            _hasCandidate = true;
            _heroImage.sprite = heroSprite;
            _evolutionText.text = $"{candidate.EvolutionLevel + 1}단계";
            gameObject.SetActive(true);
        }

        public void Clear()
        {
            _candidate = default;
            _hasCandidate = false;

            if (_heroImage != null)
                _heroImage.sprite = null;
            if (_evolutionText != null)
                _evolutionText.text = string.Empty;

            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (_hasCandidate)
                OnClick?.Invoke(_candidate);
        }
    }
}
