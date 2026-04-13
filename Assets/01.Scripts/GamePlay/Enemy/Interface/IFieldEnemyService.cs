using System;
using System.Collections.Generic;

namespace Enemies
{
    public interface IFieldEnemyService
    {
        int GetActiveEnemyCount { get; }
        IReadOnlyList<Enemy> GetAllFieldEnemy { get; }
        bool IsAliveBoss { get; }

        event Action<int> OnChangedActiveEnemyCount;

        event Action OnDeathAllEnemy;
        event Action OnDeathBossEnemy;

        void AddFieldEnemy(Enemy enemy);
    }
}
