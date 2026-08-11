using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;
using WhatMerge.Infrastructure;

namespace WhatMerge.Stage
{
    public class StagePresenter
    {
        private IWaveInfoProvider _model;
        private IStageView _view;
        private MidBossInfoPopup _popup;
        private IStageService _stageService;
        private MimicEntryData _currentMidBossData;
        private int _midBossRewardAmount;
        private IEnemyDataRepository _enemyDataRepository;
        private ISpriteRepository _enemySpriteRepository;

        public void Init(
            IStageService stageService,
            IWaveInfoProvider waveInfoProvider,
            IStageView viewer,
            MidBossInfoPopup popup,
            IEnemyDataRepository enemyDataRepository,
            ISpriteRepository enemySpriteRepository)
        {
            _model = waveInfoProvider;
            _view = viewer;
            _popup = popup;
            _stageService = stageService;
            _enemyDataRepository = enemyDataRepository;
            _enemySpriteRepository = enemySpriteRepository;

            _view.OnClickSpawnMidBoss += OpenMidBossPopup;

            _popup.OnCloseButton += CloseMidBossPopup;
            _popup.OnClickTryButton += SummonMidBoss;

            _stageService.OnShowMimicSpawnButton += ActiveOnMidBossButton;
            _stageService.OnHideMimicSpawnButton += DeactiveMidBossButton;

            _model.OnChangeCurrentWave += UpdateWave;
            _model.OnChangeRemainTime += UpdateWaveTime;
            _model.OnChangeAliveEnemy += UpdateActiveEnemyCount;
        }

        private void ActiveOnMidBossButton(MimicEntryData midBossData, int rewardAmount)
        {
            _currentMidBossData = midBossData;
            _midBossRewardAmount = rewardAmount;
            _view.ShowMiddBossButton();
            Debug.Log("ActiveOnMidBossButton");
        }
        private void DeactiveMidBossButton()
        {
            _view.HideMidBossButton();
            CloseMidBossPopup();
        }
        private void SummonMidBoss()
        {
            _stageService.SpawnMimic();
            CloseMidBossPopup();
        }

        private void OpenMidBossPopup()
        {
            var enemyData = _enemyDataRepository.GetData(_currentMidBossData.EnemyUID);
            if (enemyData == null)
                throw new InvalidOperationException($"Mimic enemy {_currentMidBossData.EnemyUID} does not exist.");

            List<Sprite> sprites = _enemySpriteRepository.GetSprites(enemyData.SpriteKey);
            if (sprites == null || sprites.Count == 0)
                throw new InvalidOperationException($"Mimic enemy {enemyData.UID} has no sprite.");

            string count = _midBossRewardAmount.ToString();

            _popup.SetData(
                sprites[0],
                enemyData.Name,
                enemyData.Description,
                count);
            _popup.gameObject.SetActive(true);
        }
        private void CloseMidBossPopup()
        {
            _popup.gameObject.SetActive(false);
        }

        public void UpdateWave(int currentWave)
        {
            string waveText = $"Wave : {currentWave}";
            _view.SetCurrentWave(waveText);
        }
        public void UpdateWaveTime(float remainTime)
        {
            TimeSpan time = TimeSpan.FromSeconds((int)remainTime);
            string timeText = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            _view.SetRemainTime(timeText);
        }
        public void UpdateActiveEnemyCount(int currentCount, int maxCount)
        {
            string text = $"{currentCount} / {maxCount}";
            float ratio = currentCount / (float)maxCount;
            _view.SetActiveEnemy(text, ratio);
        }
    }
}
