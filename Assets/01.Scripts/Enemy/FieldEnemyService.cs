using System;
using System.Collections.Generic;

namespace WhatMerge.Enemies
{
    public class FieldEnemyService : IFieldEnemyService
    {
        private readonly List<Enemy> _activeEnemies = new List<Enemy>();
        private Enemy _activeBoss;

        public int GetActiveEnemyCount => _activeEnemies.Count;
        public IReadOnlyList<Enemy> GetAllFieldEnemy => _activeEnemies;
        public bool IsAliveBoss => _activeBoss != null;

        public event Action<int> OnChangedActiveEnemyCount;
        public event Action OnFieldCleared;
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
            if (!enemy.IsActive)
                throw new InvalidOperationException("Only an active enemy can be registered in the field.");
            if (enemy.Type == EnemyType.Boss && _activeBoss != null)
                throw new InvalidOperationException("Only one boss can be active at a time.");

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

        public void AddFixedValueToAllEnemies(EnemyStatType statType, float value)
        {
            ValidateStatChange(statType, value);

            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                Enemy enemy = _activeEnemies[i];

                if (enemy.IsActive)
                    enemy.AddFixedValue(statType, value);
            }
        }

        public void AddMultiplierToAllEnemies(EnemyStatType statType, float value)
        {
            ValidateStatChange(statType, value);

            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                Enemy enemy = _activeEnemies[i];

                if (enemy.IsActive)
                    enemy.AddMultiplier(statType, value);
            }
        }

        public void DeathEnemy(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));
            if (enemy.IsActive)
                throw new InvalidOperationException("An active enemy cannot be processed as dead.");

            RemoveEnemy(enemy);

            OnEnemyDeath?.Invoke(enemy);

            if (enemy.Type == EnemyType.Boss)
            {
                OnDeathBossEnemy?.Invoke(enemy);
            }
            else if (enemy.Type == EnemyType.MiddleBoss)
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

        private static void ValidateStatChange(EnemyStatType statType, float value)
        {
            if (!Enum.IsDefined(typeof(EnemyStatType), statType))
                throw new ArgumentOutOfRangeException(nameof(statType), statType, "Enemy stat type must be a defined value.");
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Enemy stat change must be finite.");
        }

        private void NotifyEnemyCountChanged()
        {
            OnChangedActiveEnemyCount?.Invoke(GetActiveEnemyCount);

            if (GetActiveEnemyCount == 0)
                OnFieldCleared?.Invoke();
        }
    }
}
