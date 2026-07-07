using Enemies;
using WhatMerge.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Infrastructure;

namespace WhatMerge.Enemies
{
    public enum EnemyType
    {
        Nomal,
        MiddleBoss,
        Boss
    }

    public class EnemySpawner : MonoBehaviour, IEnemySpawnService
    {
        [SerializeField] private Enemy _enemyPrefab;

        public event Action OnEndWaveSpawn;
        public event Action<Enemy> OnSpawnEnemy;
        public event Action<Enemy> OnReturnEnemy;

        private ObjectPool<Enemy> _enemyPool = new ObjectPool<Enemy>();

        ISpriteRepository _spriteRepository;
        IEnemyDataRepository _enemyDataRepository;
        IPathProvider _pathProvider;

        public void Init(IPathProvider pathProvider, ISpriteRepository spriteRepository, IEnemyDataRepository enemyDataRepository)
        {
            _spriteRepository = spriteRepository;
            _pathProvider = pathProvider;
            _enemyDataRepository = enemyDataRepository;

            _enemyPool.OnCreateEvent += InitializeSpawnEnemy;
            _enemyPool.Init(this.transform, _enemyPrefab, 10);
        }

        public void StartWaveEnemySpawn(EnemySpawnReceipt data)
        {
            StartCoroutine(SpawnWaveEnemy(data));
        }
        public IEnumerator SpawnWaveEnemy(EnemySpawnReceipt data)
        {
            yield return new WaitForSeconds(data.Delay);

            EnemyData enemyData = _enemyDataRepository.GetData(data.EnemyUID);

            for (int i = 0; i < data.SpawnCount; i++)
            {
                SpawnEnemy(enemyData);

                yield return new WaitForSeconds(data.SpawnInterval);
            }

            OnEndWaveSpawn?.Invoke();
        }
        public void SpawnEnemy(EnemyData enemyData)
        {
            Enemy enemy = _enemyPool.GetItem(_pathProvider.GetDestination(0));
            var sprites = _spriteRepository.GetSprites(enemyData.Name);
            enemy.Init(enemyData, sprites);
            OnSpawnEnemy?.Invoke(enemy);
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

        public void CancelWaveSpawn()
        {
            throw new NotImplementedException();
        }
    }
}