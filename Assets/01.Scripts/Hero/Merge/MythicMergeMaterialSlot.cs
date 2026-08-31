using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Heros
{
    public class MythicMergeMaterialSlot : MonoBehaviour
    {
        private static readonly Color MissingColor = new(0.45f, 0.45f, 0.45f, 0.65f);

        [SerializeField] private Image _heroImage;
        [SerializeField] private GameObject _ownedIcon;
        [SerializeField] private TMP_Text _stateText;

        public void ValidateReferences()
        {
            if (_heroImage == null || _ownedIcon == null || _stateText == null)
                throw new InvalidOperationException($"Mythic merge material slot '{name}' is not fully assigned.");
        }

        public void SetState(Sprite heroSprite, bool isOwned)
        {
            if (heroSprite == null)
                throw new ArgumentNullException(nameof(heroSprite));

            _heroImage.sprite = heroSprite;
            _heroImage.color = isOwned ? Color.white : MissingColor;
            _ownedIcon.SetActive(isOwned);
            _stateText.text = isOwned ? "보유" : "미보유";
            gameObject.SetActive(true);
        }

        public void Clear()
        {
            if (_heroImage != null)
            {
                _heroImage.sprite = null;
                _heroImage.color = Color.white;
            }
            if (_ownedIcon != null)
                _ownedIcon.SetActive(false);
            if (_stateText != null)
                _stateText.text = string.Empty;

            gameObject.SetActive(false);
        }
    }
}
