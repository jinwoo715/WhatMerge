using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Enemies
{
    public class FieldEnemyService : IFieldEnemyService
    {
        private List<Enemy> _activeEnemies = new List<Enemy>();
        private Enemy _activeBoss = null;

        public int GetActiveEnemyCount => _activeEnemies.Count;
        public IReadOnlyList<Enemy> GetAllFieldEnemy => _activeEnemies;
        bool IFieldEnemyService.IsAliveBoss => _activeBoss != null;

        public event Action<int> OnChangedActiveEnemyCount;
        public event Action OnDeathAllEnemy;
        public event Action<Enemy> OnDeathBossEnemy;
        public event Action<Enemy> OnDeathMidBossEnemy;
        public event Action<Enemy> OnEnemyDeath;
        public event Action<Enemy> OnSpawnEnemy;

        public void AddFieldEnemy(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            if (_activeEnemies.Contains(enemy))
                throw new InvalidOperationException("The enemy is already registered in the field.");

            _activeEnemies.Add(enemy);

            if (enemy.Type == EnemyType.Boss)
                _activeBoss = enemy;

            OnSpawnEnemy?.Invoke(enemy);
            OnChangedActiveEnemyCount?.Invoke(GetActiveEnemyCount);
        }

        public void RemoveFieldEnemy(Enemy enemy)
        {
            RemoveEnemy(enemy);
            NotifyEnemyCountChanged();
        }

        public void AllEnemyStatModify(EnemyStatType statType, float value)
        {
            throw new NotImplementedException();
        }

        public void DeathEnemy(Enemy enemy)
        {
            RemoveEnemy(enemy);

            OnEnemyDeath?.Invoke(enemy);

            if(enemy.Type == EnemyType.Boss)
            {
                OnDeathBossEnemy?.Invoke(enemy);
            }
            else if(enemy.Type == EnemyType.MiddleBoss)
            {
                OnDeathMidBossEnemy?.Invoke(enemy);
            }

            NotifyEnemyCountChanged();
        }

        private void RemoveEnemy(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            if (!_activeEnemies.Remove(enemy))
                throw new InvalidOperationException("The enemy is not registered in the field.");

            if (ReferenceEquals(_activeBoss, enemy))
                _activeBoss = null;
        }

        private void NotifyEnemyCountChanged()
        {
            OnChangedActiveEnemyCount?.Invoke(GetActiveEnemyCount);

            if (GetActiveEnemyCount == 0)
                OnDeathAllEnemy?.Invoke();
        }
    }
}
