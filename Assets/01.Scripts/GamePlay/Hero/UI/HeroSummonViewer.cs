using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Heros.UI
{
    public class HeroSummonViewer : MonoBehaviour
    {
        [SerializeField] private Button _spawnButton;

        public event Action OnHeroSpawn;

        public void Init()
        {
            _spawnButton.onClick.AddListener(OnClickSpawn);
        }
        private void OnClickSpawn()
        {
            OnHeroSpawn?.Invoke();
        }
    }
}
