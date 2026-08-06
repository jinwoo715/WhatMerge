using System;
using WhatMerge.Enemies;

namespace WhatMerge.Stage
{
    public class StagePresenter
    {
        private IWaveInfoProvider _model;
        private IStageView _view;
        private MidBossInfoPopup _popup;
        private IStageService _stageService;
        private MidBossData _currentMidBossData;
        private int _midBossRewardAmount;
        private IEnemyDataRepository _enemyDataRepository;

        public void Init(
            IStageService stageService,
            IWaveInfoProvider waveInfoProvider,
            IStageView viewer,
            MidBossInfoPopup popup,
            IEnemyDataRepository enemyDataRepository)
        {
            _model = waveInfoProvider;
            _view = viewer;
            _popup = popup;
            _stageService = stageService;
            _enemyDataRepository = enemyDataRepository;

            _view.OnClickSpawnMidBoss += OpenMidBossPopup;

            _popup.OnCloseButton += CloseMidBossPopup;
            _popup.OnClickTryButton += SummonMidBoss;

            _stageService.OnShowMiddleBossSpawnButton += ActiveOnMidBossButton;
            _stageService.OnHideMiddleBossSpawnButton += DeactiveMidBossButton;

            _model.OnChangeCurrentWave += UpdateWave;
            _model.OnChangeRemainTime += UpdateWaveTime;
            _model.OnChangeAliveEnemy += UpdateActiveEnemyCount;
        }

        private void ActiveOnMidBossButton(MidBossData midBossData, int rewardAmount)
        {
            _currentMidBossData = midBossData;
            _midBossRewardAmount = rewardAmount;
            _view.ShowMiddBossButton();
        }
        private void DeactiveMidBossButton()
        {
            _view.HideMidBossButton();
            CloseMidBossPopup();
        }
        private void SummonMidBoss()
        {
            _stageService.SummonMiddBoss();
            CloseMidBossPopup();
        }

        private void OpenMidBossPopup()
        {
            var enemyData = _enemyDataRepository.GetData(_currentMidBossData.MidBossUID);
            string count = _midBossRewardAmount.ToString();

            _popup.SetData(
                _currentMidBossData.IconSprite,
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
