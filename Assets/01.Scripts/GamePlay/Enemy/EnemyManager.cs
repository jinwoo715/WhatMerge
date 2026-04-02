using Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace Enemies 
{
    public interface IEnemySpawnService
    {
        event Action OnEndWaveSpawn;
        void RequestWaveStart(WaveData data);
    }

    public interface IFieldEnemyService
    {
        List<Enemy> GetAllFieldEnemy();
        event Action<int> OnSpawnEnemy;
        event Action<int> OnDieEnemy;
    }

    public class EnemyManager : MonoBehaviour, IEnemySpawnService, IFieldEnemyService
    {
        [SerializeField] private Enemy _enemyPrefab;
        [SerializeField] private EnemyData _data;

        Dictionary<string, List<Sprite>> _enemySpriteByName = new Dictionary<string, List<Sprite>>();

        private List<Enemy> _activeEnemies;

        IEnemyMapService _mapService;

        private WaveData _currentWaveData;
        
        public event Action OnEndWaveSpawn;
        public event Action<int> OnSpawnEnemy;
        public event Action<int> OnDieEnemy;
        
        public void Init(IEnemyMapService enemyMapService, int stageUID)
        {
            _mapService = enemyMapService;

            SpriteAtlas enemyAtlas = GameManager.Data.GetEnemyAtlas(stageUID);
            Sprite[] sprites = new Sprite[enemyAtlas.spriteCount];

            enemyAtlas.GetSprites(sprites);
            
            SortSprite(sprites);
        }

        private void SortSprite(Sprite[] sprites)
        {
            foreach (var sprite in sprites)
            {
                string[] spriteNames = sprite.name.Split("_");

                if (!_enemySpriteByName.ContainsKey(spriteNames[0]))
                {
                    _enemySpriteByName.Add(spriteNames[0], new List<Sprite>());
                }

                _enemySpriteByName[spriteNames[0]].Add(sprite);
            }

            foreach (var sprite in _enemySpriteByName)
            {
                sprite.Value.Sort((a,b) => a.name.CompareTo(b.name));
            }
        }

        public int uid;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SpawnEnemy(uid++);
            }
        }

        public void RequestWaveStart(WaveData data)
        {
            StartCoroutine(SpawnWaveEnemy(data));
        }

        public IEnumerator SpawnWaveEnemy(WaveData data)
        {
            yield return new WaitForSeconds(data.StartDelay);

            for (int i = 0; i < data.SpawnCount; i++)
            {
                SpawnEnemy(data.EnemyUID);
                yield return new WaitForSeconds(data.SpawnInterval);
            }

            OnEndWaveSpawn?.Invoke();
        }
        public void SpawnEnemy(int uid)
        {
            EnemyData data = GameManager.Data.GetEnemyData(uid);

            Enemy enemy = Instantiate(_enemyPrefab, this.transform);
            enemy.Initialize();
            enemy.transform.position = _mapService.EnemySpawnPosition;

            var sprites = _enemySpriteByName[data.Name];
            enemy.Init(data, sprites);

            enemy.OnReachedDestination += AssignEnemyMove;

            AssignEnemyMove(enemy);
        }

        private void AssignEnemyMove(Enemy enemy)
        {
            int currentIndex = enemy.CurrentMoveDestinationIndex;
            int nextMoveIndex = (currentIndex + 1) % _mapService.MapEnemyDestinationCount;

            Vector3 destination = _mapService.GetEnemyNextDestination(nextMoveIndex);
            enemy.Move(destination, nextMoveIndex);
        }

        public List<Enemy> GetAllFieldEnemy()
        {
            return _activeEnemies;
        }
    }
}
