using System;

namespace WhatMerge.Enemies
{
    public interface IEnemySpawnService
    {
        event Action<Enemy> OnSpawnEnemy;
        event Action<Enemy> OnReturnEnemy;
        event Action OnEndWaveSpawn;
        void StartWaveEnemySpawn(WaveData data);
    }
}
