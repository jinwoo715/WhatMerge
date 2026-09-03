using System;
using WhatMerge.Stage;
using WhatMerge.Map;

namespace WhatMerge.Enemies
{
    public interface IEnemySpawnService
    {
        event Action<Enemy> OnSpawnEnemy;
        event Action<Enemy> OnDeathEnemy;
        event Action<Enemy> OnDespawnEnemy;
        event Action<int> OnEndWaveSpawn;

        int StartWaveEnemySpawn(EnemySpawnData data);
        Enemy SpawnEnemy(int enemyUID);
        Enemy SpawnEnemy(int enemyUID, EnemyPathPosition pathPosition);
        void DespawnEnemy(Enemy enemy);
        void CancelWaveSpawn();
    }
}
