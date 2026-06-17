using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Heros
{
    public interface IHeroSummonViewer
    {
        event Action OnSpawnRandomHero;
        void SetSpawnCost(int cost);
        void SetButtonInteractable(bool isEnable);
    }

    public class HeroSummonViewer : MonoBehaviour, IHeroSummonViewer
    {
        [SerializeField] private Button _ranSpawnButton;
        [SerializeField] private TMP_Text _ranSpawnCostText;

        public event Action OnSpawnRandomHero;

        private void Awake()
        {
            _ranSpawnButton.onClick.AddListener(OnClickSpawn);
        }

        public void SetButtonInteractable(bool interactable)
        {
            _ranSpawnButton.interactable = interactable;
        }
        public void SetSpawnCost(int cost)
        {
            _ranSpawnCostText.text = cost.ToString();
        }

        private void OnClickSpawn()
        {
            OnSpawnRandomHero?.Invoke();
        }
    }
}
