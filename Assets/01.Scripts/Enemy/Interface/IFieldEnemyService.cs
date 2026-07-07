using System;
using System.Collections.Generic;

namespace WhatMerge.Enemies
{
    public interface IFieldEnemyService
    {
        int GetActiveEnemyCount { get; }
        IReadOnlyList<Enemy> GetAllFieldEnemy { get; }
        bool IsAliveBoss { get; }

        event Action<Enemy> OnEnemyDeath;
        event Action<Enemy> OnSpawnEnemy;

        event Action<int> OnChangedActiveEnemyCount;

        event Action OnDeathAllEnemy;
        event Action<Enemy> OnDeathBossEnemy;
        event Action<Enemy> OnDeathMidBossEnemy;

        void AddFieldEnemy(Enemy enemy);

        void AllEnemyStatModify(EnemyStatType statType, float value);
    }
}
