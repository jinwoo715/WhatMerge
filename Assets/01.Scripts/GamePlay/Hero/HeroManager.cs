using Combat;
using Entity;
using Map;
using Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.U2D;

namespace Heros
{
    public interface ITileHeroHandler
    {
        void OnPointDown(Tile tile);
        void OnPointUp(Tile tile);
    }

    public interface IHeroSummonService
    {
        int SpawnedCount { get; }
        bool TrySpawnRandomHero();
        event Action<Hero> OnSpawndRanHero;
    }

    public interface IFieldHeroService
    {
        int GetActiveHeroCount { get; }
        IReadOnlyList<Hero> GetAllFieldHero { get; }
        void AddFieldEnemy(Hero enemy);
    }

    public struct HeroSkillBundle
    {
        public int BaseSkill;
        public int FirstSkill;
        public int SecondSkill;
        public int SpecialSkill;

        public HeroSkillBundle(int baseSkill, int first, int second, int special)
        {
            BaseSkill = baseSkill;
            FirstSkill = first;
            SecondSkill = second;
            SpecialSkill = special;
        }
    }

    public class MergeData
    {
        public int First;
        public int Second;
        public int Result;
    }

    public class MergeRepository
    {
        Dictionary<(int, int), int> _mergeData = new Dictionary<(int, int), int>();

        public void Init(List<MergeData> mergeDatas)
        {
            for (int i = 0; i < mergeDatas.Count; i++)
            {
                MergeData data = mergeDatas[i];

                var key = SortUID(data.First, data.Second);

                _mergeData.Add(key, data.Result);
            }
        }

        //Evolution Level이 같은가 
        public int GetMergeResult(int first, int second)
        {
            var key = SortUID(first, second);

            if(_mergeData.TryGetValue(key, out int value))
            {
                return value;
            }

            return 0;
        }
        public bool IsCanMerge(int first, int second)
        {
            var key = SortUID(first, second);

            return _mergeData.ContainsKey(key);
        }

        public (int, int) SortUID(int first, int second)
        {
            int min = Mathf.Min(first, second);
            int max = Mathf.Max(first, second);

            return (min, max);
        }
    }

    public class HeroOverlapProcessor
    {
        private MergeRepository _mergeRepository;

        public void Init(MergeRepository mergeRepository)
        {
            _mergeRepository = mergeRepository;
        }

        public int GetMergeHeroUID(int first, int second)
        {
            return _mergeRepository.GetMergeResult(first, second);
        }

        public EHeroOverlapResult OverlapHero(IHeroInfoProvider first, IHeroInfoProvider second)
        {
            if (first.EvolutionLevel != second.EvolutionLevel)
                return EHeroOverlapResult.None;

            //게임 내에서 진화 레벨은 같다.
            if (first.UID == second.UID)
                return EHeroOverlapResult.Evolution;

            if(_mergeRepository.IsCanMerge(first.UID, second.UID))
            {
                return EHeroOverlapResult.Merge;
            }

            return EHeroOverlapResult.None;
        }
    }

    public enum EHeroOverlapResult
    {
        None,
        Evolution,
        Merge
    }

    public class HeroManager : MonoBehaviour, IHeroSummonService, ITileHeroHandler
    {
        private HeroDeck _heroDeck;

        private Dictionary<IReadOnlyTile, Hero> _fieldHeros = new Dictionary<IReadOnlyTile, Hero>();
        private Hero _clickedHero = null;

        IHeroMapService _heroMapService;

        private HeroSpawner _heroSpawner;

        public event Action<Hero> OnSpawndRanHero;

        private int _spawnRanHeroCount;
        public int SpawnedCount => _spawnRanHeroCount;
        private HeroOverlapProcessor _overlapProcessor;
        public void Init(HeroOverlapProcessor heroOverlapProcessor, IHeroMapService heroMapService, HeroDeck heroDeck, HeroSpawner heroSpawner)
        {
            _heroMapService = heroMapService;
            _heroDeck = heroDeck;
            _heroSpawner = heroSpawner;
            _overlapProcessor = heroOverlapProcessor;
        }
        public bool TrySpawnRandomHero()
        {
            if (_heroMapService.TryGetNextHeroTile(out Tile tile))
            {
                int heroUid = _heroDeck.RanHeroUID();

                SpawnHero(heroUid, tile);

                return true;
            }
            return false;
        }
        public void SpawnHero(int uid, Tile tile)
        {
            Vector3 pos = _heroMapService.GetTileWorldPosition(tile);
            Hero hero = _heroSpawner.SpawnHero(uid, pos, 0);
            hero.SetTile(tile, pos);

            _heroMapService.OccupyHeroTile(tile);

            _fieldHeros.Add(tile, hero);

            _spawnRanHeroCount++;
        }
        public void ReturnHero(Hero hero)
        {
            IReadOnlyTile tile = hero.OccupiedTile;
            _fieldHeros.Remove(tile);
            _heroMapService.FreeHeroTile(tile);
            _heroSpawner.ReturnHero(hero);
        }
        public void OnPointUp(Tile tile)
        {
            if (_clickedHero == null) return;

            if (_fieldHeros.TryGetValue(tile, out var hero))
            {
                if (_clickedHero == hero)
                {
                    //Info 보여주기
                    Debug.Log("영웅 클릭!");
                }
                else
                {
                    var result = _overlapProcessor.OverlapHero(_clickedHero, hero);

                    switch (result)
                    {
                        case EHeroOverlapResult.None:
                            var startTile = _clickedHero.OccupiedTile;
                            var endTile = hero.OccupiedTile;

                            _fieldHeros.Remove(startTile);
                            _fieldHeros.Remove(endTile);

                            _fieldHeros.Add(endTile, _clickedHero);
                            _fieldHeros.Add(startTile, hero);

                            _clickedHero.SetTile(endTile, _heroMapService.GetTileWorldPosition(endTile));
                            hero.SetTile(startTile, _heroMapService.GetTileWorldPosition(startTile));

                            break;
                        case EHeroOverlapResult.Evolution:
                            ReturnHero(_clickedHero);
                            hero.EvolutionUp();

                            break;
                        case EHeroOverlapResult.Merge:

                            ReturnHero(_clickedHero);
                            ReturnHero(hero);

                            int uid = _overlapProcessor.GetMergeHeroUID(_clickedHero.UID, hero.UID);
                            SpawnHero(uid, tile);

                            Debug.Log("합췌!!");
                            break;
                    }
                }
            }
            else 
            {
                SetHeroPosition(tile, _clickedHero);
            }
        }
        public void OnPointDown(Tile tile)
        {
            if (_fieldHeros.TryGetValue(tile, out var hero))
            {
                _clickedHero = hero;
            }
        }
        private void SetHeroPosition(IReadOnlyTile tile, Hero hero)
        {
            if (hero.OccupiedTile != null)
                _heroMapService.FreeHeroTile(hero.OccupiedTile);

            if (_fieldHeros.ContainsKey(hero.OccupiedTile))
                _fieldHeros.Remove(hero.OccupiedTile);

            _heroMapService.OccupyHeroTile(tile);
            _fieldHeros.Add(tile, hero);

            hero.SetTile(tile, _heroMapService.GetTileWorldPosition(tile));
        }
    }
}
