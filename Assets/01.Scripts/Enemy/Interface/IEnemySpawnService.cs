using System;
using WhatMerge.Stage;

namespace WhatMerge.Enemies
{
    public interface IEnemySpawnService
    {
        event Action<Enemy> OnSpawnEnemy;
        event Action<Enemy> OnDeathEnemy;
        event Action<Enemy> OnDespawnEnemy;
        event Action OnEndWaveSpawn;

        void StartWaveEnemySpawn(EnemySpawnData data);
        Enemy SpawnEnemy(int enemyUID);
        void DespawnEnemy(Enemy enemy);
        void CancelWaveSpawn();
    }
}
