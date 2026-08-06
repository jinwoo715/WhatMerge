using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemies;
using System;
using WhatMerge.Enemies;

namespace WhatMerge.Stage
{
    //스테이지 시작
    public class StageManager : MonoBehaviour, IStageService, IWaveInfoProvider, IMidBossChallengeInfo
    {
        private enum MidBossState
        {
            Cooldown,
            Available,
            Active,
            Exhausted
        }

        [SerializeField] private StageData _currentStageData;
        [SerializeField] private int _startIndex;

        private int _currentWaveIndex;
        private int _lastWaveIndex;

        private float _currentTime;

        private int _maxEnemyCount;

        private IEnemySpawnService _enemySpawnService;
        private IFieldEnemyService _fieldEnemyService;
        private IGameGoldService _gameCurrencyService;

        public event Action OnStageClear;
        public event Action OnStageFail;

        public event Action<int> OnChangeCurrentWave;
        public event Action<float> OnChangeRemainTime;
        public event Action<int, int> OnChangeAliveEnemy;
        public event Action<MidBossData, int> OnShowMiddleBossSpawnButton;
        public event Action OnHideMiddleBossSpawnButton;
        public event Action<Enemy, float, float> OnMidBossTimeChanged;
        public event Action<Enemy> OnMidBossChallengeEnded;

        //스테이지 시작 -> 보스 스테이지, 일반 스테이지 확인
        //웨이브 시작

        //웨이브 성공
        //웨이브 실패

        private bool _isBossWave = false;
        private int _remainSpawn = 0;

        private MidBossData _currentMidBoss;
        private int _midBossIndex = 0;
        private float _midBossCooldown;
        private float _midBossRemainTime;
        private Enemy _activeMidBoss;
        private MidBossState _midBossState;

        public void Init(
            IEnemySpawnService enemySpawnService,
            IFieldEnemyService fieldEnemyService,
            IGameGoldService gameCurrencyService)
        {
            _enemySpawnService = enemySpawnService ?? throw new ArgumentNullException(nameof(enemySpawnService));
            _fieldEnemyService = fieldEnemyService ?? throw new ArgumentNullException(nameof(fieldEnemyService));
            _gameCurrencyService = gameCurrencyService ?? throw new ArgumentNullException(nameof(gameCurrencyService));

            _enemySpawnService.OnEndWaveSpawn += OnSpawnEndHandle;
            _fieldEnemyService.OnDeathMidBossEnemy += MidBossHandle;

            _fieldEnemyService.OnChangedActiveEnemyCount += HandleFieldEnemy;

            _currentWaveIndex = _startIndex;
            _lastWaveIndex = _currentStageData.GetLastWave;

            _currentTime = 3;

            _maxEnemyCount = _currentStageData.MaxAcceptableEnemyCount;
            _midBossState = HasRemainingMidBoss() ? MidBossState.Cooldown : MidBossState.Exhausted;

            OnChangeAliveEnemy?.Invoke(0, _maxEnemyCount);
        }


        private void Update()
        {
            UpdateWaveTime();
            UpdateMidBoss();
        }
        private void UpdateWaveTime()
        {
            _currentTime -= Time.deltaTime;

            OnChangeRemainTime?.Invoke(_currentTime);

            if (_currentTime <= 0)
            {
                OnTimeOut();
            }
        }
        private void OnTimeOut()
        {
            if (_isBossWave && _fieldEnemyService.IsAliveBoss)
            {
                Debug.Log("게임 종료");
                FailStage();
            }
            else
            {
                Debug.Log("다음 웨이브 시작");
                StartNextWave();
            }
        }

        private void StartNextWave()
        {
            _currentWaveIndex++;

            if(_currentWaveIndex > _lastWaveIndex)
            {
                ClearStage();
                return;
            }

            WaveSpawnRequest();

            OnChangeCurrentWave?.Invoke(_currentWaveIndex);
            OnChangeRemainTime?.Invoke(_currentTime);
        }



        private void WaveSpawnRequest()
        {
            if (_currentStageData.TryBossWave(_currentWaveIndex, out var data))
            {
                RequestBossWave(data);
                _currentTime = _currentStageData.BossWaveTime;
            }
            else if (_currentStageData.TryNomalWave(_currentWaveIndex, out var waveData))
            {
                RequestNomalWave(waveData);
                _currentTime = _currentStageData.NomalWaveTime;
            }
            else
            {
                throw new InvalidOperationException($"Empty Wave Data {_currentWaveIndex}");
            }
        }

        private void OnSpawnEndHandle()
        {
            _remainSpawn--;

            if (_remainSpawn < 0)
                throw new InvalidOperationException("Invalid Spawn Count");
        }

        private void RequestNomalWave(WaveData waveData)
        {
            foreach (var wave in waveData.SpawnDatas)
            {
                _enemySpawnService.StartWaveEnemySpawn(wave);
                _remainSpawn++;
            }
        }

        private void RequestBossWave(BossWaveData bossWaveData)
        {
            _enemySpawnService.StartWaveEnemySpawn(new EnemySpawnData(bossWaveData.BossUID, 1, 0, 0));
            _remainSpawn++;
        }

        private void ClearStage()
        {
            OnStageClear?.Invoke();
        }
        private void HandleFieldEnemy(int aliveEnemy)
        {
            OnChangeAliveEnemy?.Invoke(aliveEnemy, _maxEnemyCount);

            if (aliveEnemy > _maxEnemyCount)
                FailStage();

            if (aliveEnemy == 0 && _remainSpawn == 0)
                SkipRemainTime();
        }
        private void FailStage()
        {
            _enemySpawnService.CancelWaveSpawn();
            OnStageFail?.Invoke();
        }
        private void SkipRemainTime()
        {
            if (_currentTime >= 5)
                _currentTime = 5;
        }


        public void SummonMiddBoss()
        {
            if (_midBossState != MidBossState.Available || _currentMidBoss == null)
                throw new InvalidOperationException("A middle boss is not available for summoning.");

            MiddleBossData middleBossData = _currentStageData.MiddleBossData
                ?? throw new InvalidOperationException("Middle boss data is missing.");

            if (middleBossData.TimeLimit <= 0f)
                throw new InvalidOperationException("The middle boss time limit must be greater than zero.");

            Enemy enemy = _enemySpawnService.SpawnEnemy(_currentMidBoss.MidBossUID);

            if (enemy.Type != EnemyType.MiddleBoss)
            {
                _enemySpawnService.DespawnEnemy(enemy);
                throw new InvalidOperationException(
                    $"Enemy {enemy.UID} is configured as {enemy.Type}, not {EnemyType.MiddleBoss}.");
            }

            _activeMidBoss = enemy;
            _midBossRemainTime = middleBossData.TimeLimit;
            _midBossState = MidBossState.Active;

            OnHideMiddleBossSpawnButton?.Invoke();
            OnMidBossTimeChanged?.Invoke(enemy, _midBossRemainTime, middleBossData.TimeLimit);
        }

        private void MidBossHandle(Enemy enemy)
        {
            if (_midBossState != MidBossState.Active || !ReferenceEquals(enemy, _activeMidBoss))
                return;

            _gameCurrencyService.GainMoney(_currentStageData.MiddleBossData.RewardAmount);
            FinishMidBossChallenge(enemy);
        }

        private void UpdateMidBoss()
        {
            if (_midBossState == MidBossState.Cooldown)
            {
                UpdateMidBossCooldown();
                return;
            }

            if (_midBossState == MidBossState.Active)
                UpdateMidBossChallenge();
        }

        private void UpdateMidBossCooldown()
        {
            _midBossCooldown += Time.deltaTime;

            if (_midBossCooldown < _currentStageData.MiddleBossData.CoolTime)
                return;

            if (!HasRemainingMidBoss())
            {
                _midBossState = MidBossState.Exhausted;
                return;
            }

            _currentMidBoss = _currentStageData.MiddleBossData.MidBossDatas[_midBossIndex]
                ?? throw new InvalidOperationException($"Middle boss data at index {_midBossIndex} is null.");
            _midBossState = MidBossState.Available;
            OnShowMiddleBossSpawnButton?.Invoke(
                _currentMidBoss,
                _currentStageData.MiddleBossData.RewardAmount);
        }

        private void UpdateMidBossChallenge()
        {
            _midBossRemainTime = Mathf.Max(0f, _midBossRemainTime - Time.deltaTime);
            OnMidBossTimeChanged?.Invoke(
                _activeMidBoss,
                _midBossRemainTime,
                _currentStageData.MiddleBossData.TimeLimit);

            if (_midBossRemainTime > 0f)
                return;

            Enemy expiredMidBoss = _activeMidBoss;
            FinishMidBossChallenge(expiredMidBoss);
            _enemySpawnService.DespawnEnemy(expiredMidBoss);
        }

        private void FinishMidBossChallenge(Enemy enemy)
        {
            OnMidBossChallengeEnded?.Invoke(enemy);

            _activeMidBoss = null;
            _currentMidBoss = null;
            _midBossRemainTime = 0f;
            _midBossCooldown = 0f;
            _midBossIndex++;
            _midBossState = HasRemainingMidBoss()
                ? MidBossState.Cooldown
                : MidBossState.Exhausted;
        }

        private bool HasRemainingMidBoss()
        {
            return _currentStageData.MiddleBossData != null
                && _currentStageData.MiddleBossData.MidBossDatas != null
                && _midBossIndex < _currentStageData.MiddleBossData.MidBossDatas.Count;
        }

        //중간 보스 소환 가능
        //버튼 활성화 (이미지)
        //버튼 클릭 
        //정보 표시 - 뷰어 열림 (이미지, 이름, 제한시간, 보상, 도전하기 버튼)
        //도전하기 클릭
        //소환

        //잡으면 보상
        //못 잡으면 그대로 사라짐
    }
}
