using System;
using UnityEngine;

namespace Stage
{
    public class StagePresenter
    {
        private IWaveInfoProvider _model;
        private IStageView _view;

        public void Init(IWaveInfoProvider waveInfoProvider, IStageView viewer)
        {
            _model = waveInfoProvider;
            _view = viewer;

            _model.OnChangeCurrentWave += UpdateWave;
            _model.OnChangeRemainTime += UpdateWaveTime;
            _model.OnChangeAliveEnemy += UpdateActiveEnemyCount;
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
