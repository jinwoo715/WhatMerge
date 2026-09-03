using System;
using System.Collections.Generic;

namespace WhatMerge.Enemies
{
    public interface IFieldEnemyService
    {
        int GetActiveEnemyCount { get; }
        IReadOnlyList<Enemy> GetAllFieldEnemy { get; }
        int AliveBossCount { get; }

        event Action<Enemy> OnEnemyDeath;
        event Action<Enemy> OnSpawnEnemy;
        event Action<Enemy> OnEnemyRemoved;

        event Action<int> OnChangedActiveEnemyCount;

        event Action OnFieldCleared;
        event Action<Enemy> OnDeathBossEnemy;
        event Action<Enemy> OnDeathMidBossEnemy;

        void AddFieldEnemy(Enemy enemy);
        void RemoveFieldEnemy(Enemy enemy);
        IReadOnlyList<Enemy> GetEnemiesByUID(int enemyUID);
        IDisposable DeferEnemyCountNotifications();

        void AddFixedValueToAllEnemies(EnemyStatType statType, float value);
        void AddMultiplierToAllEnemies(EnemyStatType statType, float value);
    }
}
