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

        event Action OnFieldCleared;
        event Action<Enemy> OnDeathBossEnemy;
        event Action<Enemy> OnDeathMidBossEnemy;

        void AddFieldEnemy(Enemy enemy);
        void RemoveFieldEnemy(Enemy enemy);

        void AddFixedValueToAllEnemies(EnemyStatType statType, float value);
        void AddMultiplierToAllEnemies(EnemyStatType statType, float value);
    }
}
