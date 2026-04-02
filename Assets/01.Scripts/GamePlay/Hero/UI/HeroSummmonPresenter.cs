using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Heros.UI
{
    public class HeroSummmonPresenter : MonoBehaviour
    {
        [SerializeField] private HeroSummonViewer _viewer;
        
        private IHeroSpawnService _heroSpawnService;

        public void Init(IHeroSpawnService heroSpawnService)
        {
            _heroSpawnService = heroSpawnService;

            _viewer.Init();
            _viewer.OnHeroSpawn += HeroSpawn;
        }
        private void HeroSpawn()
        {
            Debug.Log("Presenter");
            _heroSpawnService.SpawnRandomHero();
        }
    }
}
