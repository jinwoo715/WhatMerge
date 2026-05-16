using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    public class EnemyTracker : IFieldEnemyService
    {
        private List<Enemy> _activeEnemies = new List<Enemy>();
        private Enemy _activeBoss = null;

        public int GetActiveEnemyCount => _activeEnemies.Count;
        public IReadOnlyList<Enemy> GetAllFieldEnemy => _activeEnemies;
        bool IFieldEnemyService.IsAliveBoss => _activeBoss != null;

        public event Action<int> OnChangedActiveEnemyCount;
        public event Action OnDeathBossEnemy;
        public event Action OnDeathAllEnemy;
        public event Action<Enemy> OnEnemyDeath;
        public event Action<Enemy> OnSpawnEnemy;

        public void AddFieldEnemy(Enemy enemy)
        {
            _activeEnemies.Add(enemy);

            enemy.OnDeath += DeathEnemy;

            if (enemy.IsBoss)
                _activeBoss = enemy;

            OnChangedActiveEnemyCount?.Invoke(GetActiveEnemyCount);
        }

        public void AllEnemyStatModify(EEnemyStatType statType, float value)
        {
            throw new NotImplementedException();
        }

        public void DeathEnemy(Enemy enemy)
        {
            _activeEnemies.Remove(enemy);

            OnEnemyDeath?.Invoke(enemy);

            enemy.OnDeath -= DeathEnemy;

            if (_activeBoss == enemy)
            {
                _activeBoss = null;
                OnDeathBossEnemy?.Invoke();
            }

            OnChangedActiveEnemyCount?.Invoke(GetActiveEnemyCount);

            if (GetActiveEnemyCount == 0)
                OnDeathAllEnemy?.Invoke();
        }
    }
}
