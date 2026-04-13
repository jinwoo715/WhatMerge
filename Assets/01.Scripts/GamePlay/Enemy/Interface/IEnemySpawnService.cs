using System;

namespace Enemies
{
    public interface IEnemySpawnService
    {
        event Action<Enemy> OnSpawnEnemy;
        event Action OnEndWaveSpawn;
        void StartWaveEnemySpawn(WaveData data);
    }
}
