using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemies;
using System;
using WhatMerge.Enemies;

namespace WhatMerge.Stage
{
    public class StageManager : MonoBehaviour, IStageService, IWaveInfoProvider
    {
        public StageData _currentStageData;
        public int StartIndex = 1;

        private StageState _stageState = StageState.None;
        private WaveType _waveType = WaveType.Nomal;

        private int _currentWave;
        private int _maxWaveIndex;
        private float _currentTimer;
        private int _maxEnemyCount;

        private IEnemySpawnService _enemySpawnService;
        private IFieldEnemyService _fieldEnemyService;

        public event Action OnStageClear;
        public event Action OnStageFail;

        public event Action<int> OnChangeCurrentWave;
        public event Action<float> OnChangeRemainTime;
        public event Action<int, int> OnChangeAliveEnemy;

        private List<WaveData> _activeWaves = new List<WaveData>();

        public void Init(IEnemySpawnService enemySpawnService, IFieldEnemyService fieldEnemyService, StageSettingConfig settingConfig)
        {
            _enemySpawnService = enemySpawnService;
            _fieldEnemyService = fieldEnemyService;

            _maxEnemyCount = _currentStageData.MaxAcceptableEnemyCount;

            _fieldEnemyService.OnChangedActiveEnemyCount += HandleEnemyCount;
            _fieldEnemyService.OnDeathBossEnemy += HandleBossDeath;

            SetMaxWaveIndex();
        }

        private void SetMaxWaveIndex()
        {
            foreach (var wave in _currentStageData.WaveList)
            {
                if (wave.EndWave > _maxWaveIndex)
                    _maxWaveIndex = wave.EndWave;
            }

            foreach (var wave in _currentStageData.BossWave)
            {
                if (wave.Wave > _maxWaveIndex)
                    _maxWaveIndex = wave.Wave;
            }
        }

        public void StartStage()
        {
            EnterWave(StartIndex);
        }
        private void EnterWave(int wave)
        {
            if(wave > _maxWaveIndex)
            {
                ClearStage();
                return;
            }

            _currentWave = wave;

            bool isBoss = IsBossWave();

            _waveType = isBoss ? WaveType.Boss : WaveType.Nomal;
            _currentTimer = _currentStageData.WaveTime;

            UpdateWaveList();

            if (isBoss)
                RequestBossEnemySpawn();

            RequestNomalEnemySpawn();

            OnChangeCurrentWave?.Invoke(_currentWave);
            OnChangeRemainTime?.Invoke(_currentTimer);
        }
        private void UpdateWaveList()
        {
            foreach (var wave in _currentStageData.WaveList)
            {
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
        private void RequestNomalEnemySpawn()
        {
            for (int i = 0; i < _activeWaves.Count; i++)
            {
                var wave = _activeWaves[i];

                EnemySpawnReceipt spawnReceipt = new EnemySpawnReceipt();
                spawnReceipt.Delay = wave.StartDelay;
                spawnReceipt.SpawnCount = wave.SpawnCount;
                spawnReceipt.SpawnInterval = wave.SpawnInterval;
                spawnReceipt.EnemyUID = wave.EnemyUID;

                _enemySpawnService.StartWaveEnemySpawn(spawnReceipt);
            }
        }

        private bool IsBossWave()
        {
            return _currentStageData.BossWave.Find((x) => x.Wave == _currentWave) != null;
        }
        private void RequestBossEnemySpawn()
        {
            BossWaveData boss = _currentStageData.BossWave.Find(x => x.Wave == _currentWave);

            var receipt = new EnemySpawnReceipt
            {
                EnemyUID = boss.EnemyUID,
                SpawnCount = 1,
                SpawnInterval = 0,
                Delay = 0
            };

            _enemySpawnService.StartWaveEnemySpawn(receipt);
        }
        
        private void Update()
        {
            if (_stageState != StageState.Running)
                return;

            _currentTimer -= Time.deltaTime;

            OnChangeRemainTime(_currentTimer);

            if(_currentTimer < 0)
            {
                if (_waveType == WaveType.Boss)
                    FailStage();
                else
                    EnterWave(_currentWave + 1);
            }
        }

        private void FailStage()
        {
            _stageState = StageState.Failed;
            _enemySpawnService.CancelWaveSpawn();
            OnStageFail?.Invoke();
        }
        private void ClearStage()
        {
            _stageState = StageState.Clear;
            OnStageClear?.Invoke();
        }

        private void HandleEnemyCount(int aliveEnemy)
        {
            OnChangeAliveEnemy?.Invoke(aliveEnemy, _maxEnemyCount);

            if (aliveEnemy > _maxEnemyCount)
                FailStage();

            if (aliveEnemy == 0)
                ProcessRemainTime();
        }

        private void HandleBossDeath(Enemy enemy)
        {
            if (_waveType == WaveType.Boss)
                _waveType = WaveType.Nomal;
        }
        private void ProcessRemainTime()
        {
            if (_currentTimer >= 5)
                _currentTimer = 5;
        }
        public void SummonMiddBoss()
        {
            
        }
    }
}
