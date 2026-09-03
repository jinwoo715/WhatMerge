using System;
using System.Collections.Generic;

namespace WhatMerge.Enemies
{
    public class FieldEnemyService : IFieldEnemyService
    {
        private readonly List<Enemy> _activeEnemies = new List<Enemy>();
        private readonly HashSet<Enemy> _activeBosses = new HashSet<Enemy>();
        private readonly Dictionary<int, List<Enemy>> _activeEnemiesByUID =
            new Dictionary<int, List<Enemy>>();

        private int _countNotificationDeferralDepth;
        private bool _countNotificationPending;

        public int GetActiveEnemyCount => _activeEnemies.Count;
        public IReadOnlyList<Enemy> GetAllFieldEnemy => _activeEnemies;
        public int AliveBossCount => _activeBosses.Count;

        public event Action<int> OnChangedActiveEnemyCount;
        public event Action OnFieldCleared;
        public event Action<Enemy> OnDeathBossEnemy;
        public event Action<Enemy> OnDeathMidBossEnemy;
        public event Action<Enemy> OnEnemyDeath;
        public event Action<Enemy> OnSpawnEnemy;
        public event Action<Enemy> OnEnemyRemoved;

        public void AddFieldEnemy(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            if (_activeEnemies.Contains(enemy))
                throw new InvalidOperationException("The enemy is already registered in the field.");
            if (!enemy.IsActive)
                throw new InvalidOperationException("Only an active enemy can be registered in the field.");
            _activeEnemies.Add(enemy);

            if (!_activeEnemiesByUID.TryGetValue(enemy.UID, out List<Enemy> enemiesByUID))
            {
                enemiesByUID = new List<Enemy>();
                _activeEnemiesByUID.Add(enemy.UID, enemiesByUID);
            }

            enemiesByUID.Add(enemy);

            if (enemy.Type == EnemyType.Boss)
                _activeBosses.Add(enemy);

            OnSpawnEnemy?.Invoke(enemy);
            RequestEnemyCountNotification();
        }

        public void RemoveFieldEnemy(Enemy enemy)
        {
            RemoveEnemy(enemy);
            OnEnemyRemoved?.Invoke(enemy);
            RequestEnemyCountNotification();
        }

        public IReadOnlyList<Enemy> GetEnemiesByUID(int enemyUID)
        {
            return _activeEnemiesByUID.TryGetValue(enemyUID, out List<Enemy> enemies)
                ? enemies
                : Array.Empty<Enemy>();
        }

        public IDisposable DeferEnemyCountNotifications()
        {
            _countNotificationDeferralDepth++;
            return new EnemyCountNotificationScope(this);
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

            using (DeferEnemyCountNotifications())
            {
                RemoveEnemy(enemy);
                RequestEnemyCountNotification();

                try
                {
                    OnEnemyDeath?.Invoke(enemy);

                    if (enemy.Type == EnemyType.Boss)
                    {
                        OnDeathBossEnemy?.Invoke(enemy);
                    }
                    else if (enemy.Type == EnemyType.Mimic)
                    {
                        OnDeathMidBossEnemy?.Invoke(enemy);
                    }
                }
                finally
                {
                    OnEnemyRemoved?.Invoke(enemy);
                }
            }
        }

        private void RemoveEnemy(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            if (!_activeEnemies.Contains(enemy))
                throw new InvalidOperationException("The enemy is not registered in the field.");
            if (!_activeEnemiesByUID.TryGetValue(enemy.UID, out List<Enemy> enemiesByUID)
                || !enemiesByUID.Contains(enemy))
            {
                throw new InvalidOperationException("The enemy UID index is inconsistent with the field list.");
            }

            _activeEnemies.Remove(enemy);
            enemiesByUID.Remove(enemy);
            if (enemiesByUID.Count == 0)
                _activeEnemiesByUID.Remove(enemy.UID);

            _activeBosses.Remove(enemy);
        }

        private static void ValidateStatChange(EnemyStatType statType, float value)
        {
            if (!Enum.IsDefined(typeof(EnemyStatType), statType))
                throw new ArgumentOutOfRangeException(nameof(statType), statType, "Enemy stat type must be a defined value.");
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Enemy stat change must be finite.");
        }

        private void RequestEnemyCountNotification()
        {
            if (_countNotificationDeferralDepth > 0)
            {
                _countNotificationPending = true;
                return;
            }

            NotifyEnemyCountChanged();
        }

        private void EndEnemyCountNotificationDeferral()
        {
            if (_countNotificationDeferralDepth <= 0)
                throw new InvalidOperationException("Enemy count notification deferral is not active.");

            _countNotificationDeferralDepth--;
            if (_countNotificationDeferralDepth > 0 || !_countNotificationPending)
                return;

            _countNotificationPending = false;
            NotifyEnemyCountChanged();
        }

        private void NotifyEnemyCountChanged()
        {
            OnChangedActiveEnemyCount?.Invoke(GetActiveEnemyCount);

            if (GetActiveEnemyCount == 0)
                OnFieldCleared?.Invoke();
        }

        private sealed class EnemyCountNotificationScope : IDisposable
        {
            private FieldEnemyService _owner;

            public EnemyCountNotificationScope(FieldEnemyService owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_owner == null)
                    return;

                FieldEnemyService owner = _owner;
                _owner = null;
                owner.EndEnemyCountNotificationDeferral();
            }
        }
    }
}
