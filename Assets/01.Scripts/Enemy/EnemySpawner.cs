using WhatMerge.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Infrastructure;
using WhatMerge.Stage;

namespace WhatMerge.Enemies
{
    public enum EnemyType
    {
        Normal,
        MiddleBoss,
        Boss
    }

    public class EnemySpawner : MonoBehaviour, IEnemySpawnService
    {
        [SerializeField] private Enemy _enemyPrefab;

        public event Action OnEndWaveSpawn;
        public event Action<Enemy> OnSpawnEnemy;
        public event Action<Enemy> OnDeathEnemy;
        public event Action<Enemy> OnDespawnEnemy;

        private readonly ObjectPool<Enemy> _enemyPool = new ObjectPool<Enemy>();
        private readonly Dictionary<int, Coroutine> _activeWaveSpawns = new Dictionary<int, Coroutine>();

        private ISpriteRepository _spriteRepository;
        private IEnemyDataRepository _enemyDataRepository;
        private IPathProvider _pathProvider;
        private int _nextWaveSpawnId;
        private bool _initialized;

        public void Init(IPathProvider pathProvider, ISpriteRepository spriteRepository, IEnemyDataRepository enemyDataRepository)
        {
            if (_initialized)
                throw new InvalidOperationException($"{nameof(EnemySpawner)} is already initialized.");
            if (_enemyPrefab == null)
                throw new InvalidOperationException("Enemy prefab is not assigned.");

            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            _spriteRepository = spriteRepository ?? throw new ArgumentNullException(nameof(spriteRepository));
            _enemyDataRepository = enemyDataRepository ?? throw new ArgumentNullException(nameof(enemyDataRepository));

            _enemyPool.OnCreateEvent += InitializeSpawnEnemy;
            _enemyPool.Init(this.transform, _enemyPrefab, 10);
            _initialized = true;
        }

        public void StartWaveEnemySpawn(EnemySpawnData data)
        {
            EnsureInitialized();
            ValidateSpawnData(data);

            int spawnId = ++_nextWaveSpawnId;
            Coroutine coroutine = StartCoroutine(SpawnWaveEnemy(data, spawnId));
            _activeWaveSpawns.Add(spawnId, coroutine);
        }

        private IEnumerator SpawnWaveEnemy(EnemySpawnData data, int spawnId)
        {
            bool completed = false;

            try
            {
                yield return new WaitForSeconds(data.StartDelay);

                EnemyData enemyData = _enemyDataRepository.GetData(data.EnemyUID);

                for (int i = 0; i < data.SpawnCount; i++)
                {
                    if (!_activeWaveSpawns.ContainsKey(spawnId))
                        yield break;

                    SpawnEnemy(enemyData);

                    if (i < data.SpawnCount - 1)
                        yield return new WaitForSeconds(data.SpawnInterval);
                }

                completed = _activeWaveSpawns.ContainsKey(spawnId);
            }
            finally
            {
                _activeWaveSpawns.Remove(spawnId);
            }

            if (completed)
                OnEndWaveSpawn?.Invoke();
        }

        public Enemy SpawnEnemy(int enemyUID)
        {
            EnsureInitialized();

            EnemyData enemyData = _enemyDataRepository.GetData(enemyUID);
            return SpawnEnemy(enemyData);
        }

        private Enemy SpawnEnemy(EnemyData enemyData)
        {
            if (enemyData == null)
                throw new ArgumentNullException(nameof(enemyData));

            var sprites = _spriteRepository.GetSprites(enemyData.SpriteKey);
            Enemy enemy = _enemyPool.GetItem(_pathProvider.GetDestination(0));

            try
            {
                enemy.Init(enemyData, sprites);
            }
            catch
            {
                _enemyPool.ReturnItem(enemy);
                throw;
            }

            OnSpawnEnemy?.Invoke(enemy);
            return enemy;
        }

        private void InitializeSpawnEnemy(Enemy enemy)
        {
            enemy.Initialize(_pathProvider);
            enemy.OnDeath += HandleEnemyDeath;
        }

        private void HandleEnemyDeath(Enemy enemy)
        {
            try
            {
                OnDeathEnemy?.Invoke(enemy);
            }
            finally
            {
                _enemyPool.ReturnItem(enemy);
            }
        }

        public void DespawnEnemy(Enemy enemy)
        {
            EnsureInitialized();

            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            if (!enemy.IsActive)
                throw new InvalidOperationException("Only an active enemy can be despawned.");

            try
            {
                OnDespawnEnemy?.Invoke(enemy);
            }
            finally
            {
                _enemyPool.ReturnItem(enemy);
            }
        }

        public void CancelWaveSpawn()
        {
            if (_activeWaveSpawns.Count == 0)
                return;

            var coroutines = new List<Coroutine>(_activeWaveSpawns.Values);
            _activeWaveSpawns.Clear();

            for (int i = 0; i < coroutines.Count; i++)
                StopCoroutine(coroutines[i]);
        }

        private static void ValidateSpawnData(EnemySpawnData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.EnemyUID <= 0)
                throw new ArgumentOutOfRangeException(nameof(data), data.EnemyUID, "Enemy UID must be greater than zero.");
            if (data.SpawnCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(data), data.SpawnCount, "Spawn count must be greater than zero.");
            if (float.IsNaN(data.StartDelay) || float.IsInfinity(data.StartDelay) || data.StartDelay < 0f)
                throw new ArgumentOutOfRangeException(nameof(data), data.StartDelay, "Start delay must be a finite, non-negative value.");
            if (float.IsNaN(data.SpawnInterval) || float.IsInfinity(data.SpawnInterval) || data.SpawnInterval < 0f)
                throw new ArgumentOutOfRangeException(nameof(data), data.SpawnInterval, "Spawn interval must be a finite, non-negative value.");
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException($"Call {nameof(Init)} before using {nameof(EnemySpawner)}.");
        }

        private void OnDestroy()
        {
            CancelWaveSpawn();
            _enemyPool.OnCreateEvent -= InitializeSpawnEnemy;
        }
    }
}
