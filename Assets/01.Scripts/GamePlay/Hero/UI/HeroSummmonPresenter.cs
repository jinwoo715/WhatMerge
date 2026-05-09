using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Heros.UI
{
    [Serializable]
    public class HeroSummmonPresenter
    {
        private int _spawnCost;
        private int _currentCost;
        private int _increaseCost;

        //SpawnCount ¿©±â
        private IHeroSummonService _heroSpawnService;

        private IHeroSummonViewer _heroViewer;
        private IGameGoldService _economy;

        public void Init(IHeroSummonService heroSpawnService, IHeroSummonViewer heroSummonViewer, IGameGoldService economyService, GameEconomyConfig gameEconomy)
        {
            _heroSpawnService = heroSpawnService;
            _economy = economyService;
            _heroViewer = heroSummonViewer;

            _spawnCost = gameEconomy.StartSpawnCost;
            _increaseCost = gameEconomy.IncreaseSpawnCost;

            _heroViewer.OnSpawnRandomHero += SpawnRandomHero;

            UpdateSpawnCost();
        }

        private void SpawnRandomHero()
        {
            if (!HaveMoneyToSpawnCost(_currentCost))
                return;

            if(_heroSpawnService.TrySpawnRandomHero())
            {
                _economy.UseMoney(_currentCost);

                UpdateSpawnCost();
            }
        }

        private void UpdateSpawnCost()
        {
            _currentCost = _spawnCost + (_increaseCost * _heroSpawnService.SpawnedCount);
            _heroViewer.SetSpawnCost(_currentCost);
        }

        private bool HaveMoneyToSpawnCost(int spawnCost)
        {
            return _economy.CurrentMony >= spawnCost;
        }
    }
}
