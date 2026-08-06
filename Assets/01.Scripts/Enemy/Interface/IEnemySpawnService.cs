using System;
using WhatMerge.Stage;

namespace WhatMerge.Enemies
{
    public interface IEnemySpawnService
    {
        event Action<Enemy> OnSpawnEnemy;
        event Action<Enemy> OnReturnEnemy;
        event Action<Enemy> OnDespawnEnemy;
        event Action OnEndWaveSpawn;

        bool IsAliveBoss { get; }
        void StartWaveEnemySpawn(EnemySpawnData data);
        Enemy SpawnEnemy(int enemyUID);
        void DespawnEnemy(Enemy enemy);
        void CancelWaveSpawn();
    }

    public class EnemySpawnReceipt
    {
        public int EnemyUID;
        public int SpawnCount;
        public float Delay;
        public float SpawnInterval;

        public EnemySpawnReceipt(int uid, int count, float startDelay, float interval)
        {
            EnemyUID = uid;
            SpawnCount = count;
            Delay = startDelay;
            SpawnInterval = interval;
        }
    }
}
