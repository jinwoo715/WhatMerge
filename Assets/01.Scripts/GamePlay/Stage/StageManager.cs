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
        event Action OnExceedEnemyCount;
        event Action OnTimeOut;
        
        void StartStage();
    }

    public interface IWaveInfoProvider
    {
        event Action<int> OnChangeCurrentWave;
        event Action<float> OnChangeRemainTime;
        event Action<int, int> OnChangeAliveEnemy;
    }

    public class StageManager : MonoBehaviour, IStageService, IWaveInfoProvider
    {
        private StageData _currentStage;

        private bool _isStart = false;

        private float _currentTimer;
        
        private int _nomalWaveTime;
        private int _bossWaveTime;

        private int _currentWave;

        private IEnemySpawnService _enemySpawnService;
        private IFieldEnemyService _fieldEnemyService;

        public event Action OnClearAllWave;
        public event Action OnTimeOut;

        public event Action<int> OnChangeCurrentWave;
        public event Action<float> OnChangeRemainTime;
        public event Action<int, int> OnChangeAliveEnemy;
        public event Action OnExceedEnemyCount;

        public List<WaveData> _activeWaves = new List<WaveData>();

        private int _maxEnemyCount;
        private int _bossWave;

        public void Init(IEnemySpawnService enemySpawnService, IFieldEnemyService fieldEnemyService, StageData stageInfo, StageSettingConfig settingConfig)
        {
            _enemySpawnService = enemySpawnService;
            _fieldEnemyService = fieldEnemyService;
            _currentStage = stageInfo;

            _maxEnemyCount = settingConfig.MaxEnemy;
            _nomalWaveTime = settingConfig.WaveTime;
            _bossWaveTime = settingConfig.BossWaveTime;
            _bossWave = settingConfig.BossWavePivot;

            _currentWave = settingConfig.StartWaveIndex -1;

            _fieldEnemyService.OnChangedActiveEnemyCount += HandleEnemyCount;
            _fieldEnemyService.OnDeathBossEnemy += UpdateActiveWave;
        }
        private void HandleEnemyCount(int aliveEnemy)
        {
            Debug.Log(aliveEnemy);
            OnChangeAliveEnemy?.Invoke(aliveEnemy, _maxEnemyCount);

            if (aliveEnemy > _maxEnemyCount)
            {
                OnExceedEnemyCount?.Invoke();
                _isStart = false;
            }
        }
        public void StartStage()
        {
            _isStart = true;

            OnChangeCurrentWave?.Invoke(_currentWave +1);
            OnChangeAliveEnemy?.Invoke(0, _maxEnemyCount);

            _currentTimer = 3.0f;
        }
        private bool IsBossWave()
        {
            return _currentWave % _bossWave == 0;
        }
        private void UpdateActiveWave()
        {
            if (IsBossWave())
            {
                if (_fieldEnemyService.IsAliveBoss)
                {
                    _isStart = false;
                    OnTimeOut?.Invoke();
                    return;
                }
            }

            _currentWave++;

            OnChangeCurrentWave?.Invoke(_currentWave);

            SetTimer();

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
        private void SetTimer()
        {
            if (_currentWave % 10 == 0)
                _currentTimer = _bossWaveTime;
            else
                _currentTimer = _nomalWaveTime;
        }
        private bool IsInAreaWave(WaveData waveData)
        {
            return waveData.StartWave <= _currentWave && waveData.EndWave >= _currentWave;
        }
        private void Update()
        {
            if (_isStart == false) return;

            _currentTimer -= Time.deltaTime;

            OnChangeRemainTime(_currentTimer);

            if(_currentTimer < 0)
            {
                UpdateActiveWave();

                if (_isStart == false) return;

                for (int i = 0; i < _activeWaves.Count; i++)
                {
                    _enemySpawnService.StartWaveEnemySpawn(_activeWaves[i]);
                }
            }
        }
    }
}
