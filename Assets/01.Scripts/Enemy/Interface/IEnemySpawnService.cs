using System;

namespace WhatMerge.Enemies
{
    public interface IEnemySpawnService
    {
        event Action<Enemy> OnSpawnEnemy;
        event Action<Enemy> OnReturnEnemy;
        event Action OnEndWaveSpawn;
        void StartWaveEnemySpawn(EnemySpawnReceipt data);
        void CancelWaveSpawn();
    }

    public class EnemySpawnReceipt
    {
        public int EnemyUID;
        public int SpawnCount;
        public float SpawnInterval;
        public float Delay;
    }
}
