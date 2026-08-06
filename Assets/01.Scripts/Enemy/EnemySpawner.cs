using Enemies;
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
        public event Action<Enemy> OnReturnEnemy;
        public event Action<Enemy> OnDespawnEnemy;

        private ObjectPool<Enemy> _enemyPool = new ObjectPool<Enemy>();

        ISpriteRepository _spriteRepository;
        IEnemyDataRepository _enemyDataRepository;
        IPathProvider _pathProvider;

        public bool IsAliveBoss => throw new NotImplementedException();

        public void Init(IPathProvider pathProvider, ISpriteRepository spriteRepository, IEnemyDataRepository enemyDataRepository)
        {
            _spriteRepository = spriteRepository;
            _pathProvider = pathProvider;
            _enemyDataRepository = enemyDataRepository;

            _enemyPool.OnCreateEvent += InitializeSpawnEnemy;
            _enemyPool.Init(this.transform, _enemyPrefab, 10);
        }

        public void StartWaveEnemySpawn(EnemySpawnData data)
        {
            StartCoroutine(SpawnWaveEnemy(data));
        }
        public IEnumerator SpawnWaveEnemy(EnemySpawnData data)
        {
            yield return new WaitForSeconds(data.StartDelay);

            EnemyData enemyData = _enemyDataRepository.GetData(data.EnemyUID);

            for (int i = 0; i < data.SpawnCount; i++)
            {
                SpawnEnemy(enemyData);

                yield return new WaitForSeconds(data.SpawnInterval);
            }

            OnEndWaveSpawn?.Invoke();
        }
        public Enemy SpawnEnemy(int enemyUID)
        {
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

        public void InitializeSpawnEnemy(Enemy enemy)
        {
            enemy.Initialize(_pathProvider);
            enemy.OnDeath += OnEnemyDeath;
        }

        private void OnEnemyDeath(Enemy enemy)
        {
            _enemyPool.ReturnItem(enemy);
            OnReturnEnemy?.Invoke(enemy);
        }

        public void DespawnEnemy(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            if (!enemy.IsActive)
                throw new InvalidOperationException("Only an active enemy can be despawned.");

            _enemyPool.ReturnItem(enemy);
            OnDespawnEnemy?.Invoke(enemy);
        }

        public void CancelWaveSpawn()
        {
            throw new NotImplementedException();
        }
    }
}
