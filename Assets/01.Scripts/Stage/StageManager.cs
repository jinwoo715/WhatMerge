using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;

namespace WhatMerge.Stage
{
    public class StageManager : MonoBehaviour, IStageService, IWaveInfoProvider, IMidBossChallengeInfo
    {
        private enum MidBossState
        {
            Cooldown,
            Available,
            Active,
            Exhausted
        }

        private const float EmptyFieldSkipTime = 3f;

        [SerializeField] private StageData _currentStageData;

#if UNITY_EDITOR
        [SerializeField, Min(1)] private int _startIndex = 1;
#endif

        private readonly HashSet<int> _activeSpawnRequests = new HashSet<int>();
        private readonly HashSet<int> _currentWaveSpawnRequests = new HashSet<int>();

        private IEnemySpawnService _enemySpawnService;
        private IFieldEnemyService _fieldEnemyService;
        private IGameGoldService _gameCurrencyService;

        private StageState _stageState = StageState.None;
        private int _currentWaveIndex;
        private float _currentTime;
        private int _maxEnemyCount;
        private bool _initialized;

        private MiddleBossEntryData _currentMidBoss;
        private int _midBossIndex;
        private float _midBossCooldown;
        private float _midBossRemainTime;
        private Enemy _activeMidBoss;
        private MidBossState _midBossState;

        public event Action OnStageClear;
        public event Action OnStageFail;
        public event Action<int> OnChangeCurrentWave;
        public event Action<float> OnChangeRemainTime;
        public event Action<int, int> OnChangeAliveEnemy;
        public event Action<MiddleBossEntryData, int> OnShowMiddleBossSpawnButton;
        public event Action OnHideMiddleBossSpawnButton;
        public event Action<Enemy, float, float> OnMidBossTimeChanged;
        public event Action<Enemy> OnMidBossChallengeEnded;

        public void Init(
            IEnemySpawnService enemySpawnService,
            IFieldEnemyService fieldEnemyService,
            IGameGoldService gameCurrencyService,
            float startCountdown)
        {
            if (_initialized)
                throw new InvalidOperationException($"{nameof(StageManager)} is already initialized.");
            if (float.IsNaN(startCountdown) || float.IsInfinity(startCountdown) || startCountdown < 0f)
                throw new ArgumentOutOfRangeException(nameof(startCountdown), startCountdown, "Countdown must be finite and non-negative.");

            _enemySpawnService = enemySpawnService ?? throw new ArgumentNullException(nameof(enemySpawnService));
            _fieldEnemyService = fieldEnemyService ?? throw new ArgumentNullException(nameof(fieldEnemyService));
            _gameCurrencyService = gameCurrencyService ?? throw new ArgumentNullException(nameof(gameCurrencyService));

            if (_currentStageData == null)
                throw new InvalidOperationException("Current stage data is missing.");

            _currentStageData.ValidateOrThrow();

            _currentWaveIndex = GetStartWaveIndex();
            _currentTime = startCountdown;
            _maxEnemyCount = _currentStageData.MaxEnemyCount;
            _stageState = StageState.Countdown;
            _midBossState = HasRemainingMidBoss() ? MidBossState.Cooldown : MidBossState.Exhausted;

            _enemySpawnService.OnEndWaveSpawn += HandleSpawnCompleted;
            _fieldEnemyService.OnDeathMidBossEnemy += HandleMidBossDeath;
            _fieldEnemyService.OnChangedActiveEnemyCount += HandleFieldEnemyCountChanged;
            _initialized = true;

            OnChangeCurrentWave?.Invoke(_currentWaveIndex + 1);
            OnChangeRemainTime?.Invoke(_currentTime);
            OnChangeAliveEnemy?.Invoke(_fieldEnemyService.GetActiveEnemyCount, _maxEnemyCount);
        }

        private void Update()
        {
            if (!_initialized)
                return;

            switch (_stageState)
            {
                case StageState.Countdown:
                    UpdateStartCountdown();
                    break;
                case StageState.Running:
                    UpdateWaveTime();
                    if (_stageState == StageState.Running)
                        UpdateMidBoss();
                    break;
            }
        }

        private void UpdateStartCountdown()
        {
            _currentTime = Mathf.Max(0f, _currentTime - Time.deltaTime);
            OnChangeRemainTime?.Invoke(_currentTime);

            if (_currentTime > 0f)
                return;

            _stageState = StageState.Running;
            StartWave(_currentWaveIndex);
        }

        private void UpdateWaveTime()
        {
            _currentTime = Mathf.Max(0f, _currentTime - Time.deltaTime);
            OnChangeRemainTime?.Invoke(_currentTime);

            if (_currentTime <= 0f)
                ResolveWaveTimeout();
        }

        private void ResolveWaveTimeout()
        {
            WaveData wave = GetWave(_currentWaveIndex);
            if (wave.WaveType == WaveType.Boss
                && (_fieldEnemyService.AliveBossCount > 0 || _currentWaveSpawnRequests.Count > 0))
            {
                FailStage();
                return;
            }

            int nextWaveIndex = _currentWaveIndex + 1;
            if (nextWaveIndex >= _currentStageData.WaveCount)
            {
                ClearStage();
                return;
            }

            StartWave(nextWaveIndex);
        }

        private void StartWave(int waveIndex)
        {
            WaveData wave = GetWave(waveIndex);
            _currentWaveIndex = waveIndex;
            _currentWaveSpawnRequests.Clear();
            _currentTime = _currentStageData.GetWaveDuration(wave.WaveType);

            try
            {
                for (int i = 0; i < wave.SpawnDatas.Count; i++)
                {
                    int requestId = _enemySpawnService.StartWaveEnemySpawn(wave.SpawnDatas[i]);
                    if (!_activeSpawnRequests.Add(requestId))
                        throw new InvalidOperationException($"Spawn request {requestId} is already active.");

                    _currentWaveSpawnRequests.Add(requestId);
                }
            }
            catch
            {
                _enemySpawnService.CancelWaveSpawn();
                _activeSpawnRequests.Clear();
                _currentWaveSpawnRequests.Clear();
                throw;
            }

            OnChangeCurrentWave?.Invoke(_currentWaveIndex + 1);
            OnChangeRemainTime?.Invoke(_currentTime);
        }

        private WaveData GetWave(int waveIndex)
        {
            if (!_currentStageData.TryGetWave(waveIndex, out WaveData wave))
                throw new InvalidOperationException($"Stage {_currentStageData.UID} has no wave data at index {waveIndex}.");

            return wave;
        }

        private void HandleSpawnCompleted(int requestId)
        {
            if (!_activeSpawnRequests.Remove(requestId))
                return;

            _currentWaveSpawnRequests.Remove(requestId);
            TryShortenRemainTime();
        }

        private void HandleFieldEnemyCountChanged(int aliveEnemyCount)
        {
            OnChangeAliveEnemy?.Invoke(aliveEnemyCount, _maxEnemyCount);

            if (_stageState != StageState.Running && _stageState != StageState.Countdown)
                return;

            if (aliveEnemyCount > _maxEnemyCount)
            {
                FailStage();
                return;
            }

            TryShortenRemainTime();
        }

        private void TryShortenRemainTime()
        {
            if (_stageState != StageState.Running
                || _activeSpawnRequests.Count > 0
                || _fieldEnemyService.GetActiveEnemyCount > 0
                || _currentTime <= EmptyFieldSkipTime)
            {
                return;
            }

            _currentTime = EmptyFieldSkipTime;
            OnChangeRemainTime?.Invoke(_currentTime);
        }

        private void ClearStage()
        {
            if (!TryEnterTerminalState(StageState.Cleared))
                return;

            OnStageClear?.Invoke();
        }

        private void FailStage()
        {
            if (!TryEnterTerminalState(StageState.Failed))
                return;

            OnStageFail?.Invoke();
        }

        private bool TryEnterTerminalState(StageState terminalState)
        {
            if (_stageState == StageState.Cleared || _stageState == StageState.Failed)
                return false;

            _stageState = terminalState;
            _enemySpawnService.CancelWaveSpawn();
            _activeSpawnRequests.Clear();
            _currentWaveSpawnRequests.Clear();
            StopMiddleBossForStageEnd();
            return true;
        }

        public void SummonMiddleBoss()
        {
            if (_stageState != StageState.Running)
                throw new InvalidOperationException("A middle boss can only be summoned while the stage is running.");
            if (_midBossState != MidBossState.Available || _currentMidBoss == null)
                throw new InvalidOperationException("A middle boss is not available for summoning.");

            MiddleBossChallengeData middleBossData = _currentStageData.MiddleBossChallenge
                ?? throw new InvalidOperationException("Middle boss data is missing.");
            Enemy enemy = _enemySpawnService.SpawnEnemy(_currentMidBoss.EnemyUID);

            if (_stageState != StageState.Running)
            {
                if (enemy.IsActive)
                    _enemySpawnService.DespawnEnemy(enemy);
                return;
            }

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

        private void HandleMidBossDeath(Enemy enemy)
        {
            if (_stageState != StageState.Running
                || _midBossState != MidBossState.Active
                || !ReferenceEquals(enemy, _activeMidBoss))
            {
                return;
            }

            int bonusBattleCurrency = _currentStageData.MiddleBossChallenge.BonusBattleCurrency;
            if (bonusBattleCurrency > 0)
                _gameCurrencyService.GainMoney(bonusBattleCurrency);

            FinishMidBossChallenge(enemy);
        }

        private void UpdateMidBoss()
        {
            switch (_midBossState)
            {
                case MidBossState.Cooldown:
                    UpdateMidBossCooldown();
                    break;
                case MidBossState.Active:
                    UpdateMidBossChallenge();
                    break;
            }
        }

        private void UpdateMidBossCooldown()
        {
            _midBossCooldown += Time.deltaTime;
            if (_midBossCooldown < _currentStageData.MiddleBossChallenge.Cooldown)
                return;

            if (!HasRemainingMidBoss())
            {
                _midBossState = MidBossState.Exhausted;
                return;
            }

            _currentMidBoss = _currentStageData.MiddleBossChallenge.Entries[_midBossIndex]
                ?? throw new InvalidOperationException($"Middle boss data at index {_midBossIndex} is null.");
            _midBossState = MidBossState.Available;
            OnShowMiddleBossSpawnButton?.Invoke(
                _currentMidBoss,
                _currentStageData.MiddleBossChallenge.BonusBattleCurrency);
        }

        private void UpdateMidBossChallenge()
        {
            _midBossRemainTime = Mathf.Max(0f, _midBossRemainTime - Time.deltaTime);
            OnMidBossTimeChanged?.Invoke(
                _activeMidBoss,
                _midBossRemainTime,
                _currentStageData.MiddleBossChallenge.TimeLimit);

            if (_midBossRemainTime > 0f)
                return;

            Enemy expiredMidBoss = _activeMidBoss;
            FinishMidBossChallenge(expiredMidBoss);

            if (expiredMidBoss != null && expiredMidBoss.IsActive)
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

        private void StopMiddleBossForStageEnd()
        {
            OnHideMiddleBossSpawnButton?.Invoke();

            Enemy activeMidBoss = _activeMidBoss;
            if (activeMidBoss != null)
                OnMidBossChallengeEnded?.Invoke(activeMidBoss);

            _activeMidBoss = null;
            _currentMidBoss = null;
            _midBossRemainTime = 0f;
            _midBossCooldown = 0f;
            _midBossState = MidBossState.Exhausted;

            if (activeMidBoss != null && activeMidBoss.IsActive)
                _enemySpawnService.DespawnEnemy(activeMidBoss);
        }

        private bool HasRemainingMidBoss()
        {
            return _currentStageData.MiddleBossChallenge != null
                && _currentStageData.MiddleBossChallenge.Entries != null
                && _midBossIndex < _currentStageData.MiddleBossChallenge.Entries.Count;
        }

        private int GetStartWaveIndex()
        {
#if UNITY_EDITOR
            if (_startIndex <= 0 || _startIndex > _currentStageData.WaveCount)
                throw new InvalidOperationException($"Editor start wave {_startIndex} is outside the stage range.");

            return _startIndex - 1;
#else
            return 0;
#endif
        }

        private void OnDestroy()
        {
            if (!_initialized)
                return;

            _enemySpawnService.OnEndWaveSpawn -= HandleSpawnCompleted;
            _fieldEnemyService.OnDeathMidBossEnemy -= HandleMidBossDeath;
            _fieldEnemyService.OnChangedActiveEnemyCount -= HandleFieldEnemyCountChanged;
        }
    }
}
