using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemies;
using System;

namespace Stage
{
    public interface IStageService
    {
        event Action OnClearAllWave;
        event Action OnTimeOut;
        void StartStage();
    }

    public class StageManager : MonoBehaviour, IStageService
    {
        private StageData _currentStage;

        private bool _isStart = false;
        public float _waveTime;
        public float _timer;

        private int _currentWave;

        private IEnemySpawnService _enemySpawnService;

        public event Action OnClearAllWave;
        public event Action OnTimeOut;

        public List<WaveData> _activeWaves = new List<WaveData>();

        public void Init(IEnemySpawnService enemySpawnService, int stageUID)
        {
            _enemySpawnService = enemySpawnService;

            _currentStage = GameManager.Data.GetStageData(stageUID);
            _waveTime = GameManager.Data.StageConfig.WaveTime;
        }

        private void UpdateActiveWave()
        {
            foreach (var wave in _currentStage.WaveDatas)
            {
                if (wave.StartWave > _currentWave) break;

                if (IsInAreaWave(wave))
                {
                    if (!_activeWaves.Contains(wave))
                        _activeWaves.Add(wave);
                }
                else
                {
                    if (_activeWaves.Contains(wave))
                        _activeWaves.Remove(wave);
                }
            }
        }
        private bool IsInAreaWave(WaveData waveData)
        {
            return waveData.StartWave <= _currentWave && waveData.EndWave >= _currentWave;
        }

        public void StartStage()
        {
            Debug.Log("Start Stage");
            _isStart = true;
            _timer = _waveTime;
        }

        private void Update()
        {
            if (_isStart == false) return;

            _timer += Time.deltaTime;


            if(_timer >= _waveTime)
            {
                _currentWave++;
                UpdateActiveWave();

                _timer = 0;

                for (int i = 0; i < _activeWaves.Count; i++)
                {
                    _enemySpawnService.RequestWaveStart(_activeWaves[i]);
                }
            }
        }
    }
}
