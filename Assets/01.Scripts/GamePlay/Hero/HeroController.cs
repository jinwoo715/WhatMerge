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
    public interface IHeroSummonService
    {
        int SpawnedCount { get; }
        bool TrySpawnRandomHero();
        bool TrySpawnHero(int uid, int evolutionLevel);
        void SpawnHeroAtTile(int uid, int evolutionLevel, Tile tile);

        event Action<Tile, Hero> OnSpawndRanHero;
    }

    public interface IHeroTileService
    {
        void PointDownTile(Tile tile);
        void PointUpTile(Tile tile);
        void DragTile(Tile tile);
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

    public interface IHeroOverlapResult
    {
        public int GetMergeHeroUID(int first, int second);
        public EHeroOverlapResult OverlapHero(IHeroInfoProvider first, IHeroInfoProvider second);
    }

    public class HeroOverlapProcessor : IHeroOverlapResult
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

    public class FieldHeroManager
    {

    }

    public class HeroController : IFieldHeroService, IHeroTileService
    {
        private Dictionary<IReadOnlyTile, Hero> _fieldHeros = new Dictionary<IReadOnlyTile, Hero>();
        private Dictionary<(int x,int y), Hero> _fieldHero = new Dictionary<(int, int), Hero>();

        private Hero _clickedHero = null;

        public IReadOnlyList<Hero> GetAllFieldHero => throw new NotImplementedException();
        public bool IsUsableBag => CurrentUsedBagItem < TotalBagItem;
        public int TotalBagItem => 3;
        public int CurrentUsedBagItem => 0;

        private IHeroMapService _heroMapService;
        private IHeroOverlapResult _overlapProcessor;
        private ITileMarkerPresenter _markerPresenter;
        private IGameGoldService _gameGoldService;
        private IHeroSummonService _heroSpawnService;
        public event Action<Hero> OnSelectHero;
        public event Action OnChangedFieldHero;
        public event Action<Hero> OnSpawnedHero;
        public event Action<Hero> OnDestroyHero;

        public void Init(IHeroSummonService heroSpawnService, IHeroOverlapResult heroOverlapProcessor, IHeroMapService heroMapService, ITileMarkerPresenter markerPresenter, IGameGoldService gameGoldService)
        {
            _heroSpawnService = heroSpawnService;
            _gameGoldService = gameGoldService;
            _heroMapService = heroMapService;
            _overlapProcessor = heroOverlapProcessor;
            _markerPresenter = markerPresenter;
        }
        public void ReturnHero(Hero hero)
        {
            hero.Return();
            hero.OnReturn -= ClearHero;
        }

        public void ClearHero(Hero hero)
        {
            IReadOnlyTile tile = hero.OccupiedTile;
            _fieldHeros.Remove(tile);
            _fieldHero.Remove((tile.X, tile.Y));
            _heroMapService.FreeHeroTile(tile);
        }

        public void SetHeroPosition(IReadOnlyTile tile, Hero hero)
        {
            if (hero.OccupiedTile != null)
                _heroMapService.FreeHeroTile(hero.OccupiedTile);

            if (_fieldHeros.ContainsKey(hero.OccupiedTile))
            {
                _fieldHeros.Remove(hero.OccupiedTile);
                _fieldHero.Remove((tile.X, tile.Y));
            }

            _heroMapService.OccupyHeroTile(tile);
            _fieldHeros.Add(tile, hero);
            _fieldHero.Add((tile.X, tile.Y), hero);

            hero.SetTile(tile, _heroMapService.GetTileWorldPosition(tile));
        }
        public void AddFieldHero(Tile tile, Hero hero)
        {
            _fieldHeros.Add(tile, hero);
            _fieldHero.Add((tile.X, tile.Y), hero);

            hero.OnReturn += ClearHero;
        }

        public void PointDownTile(Tile tile)
        {
            if (_fieldHeros.TryGetValue(tile, out var hero))
            {
                _clickedHero = hero;
                _markerPresenter.ShowTileMarker(tile);
            }
        }
        public void PointUpTile(Tile tile)
        {
            if (_clickedHero == null) return;

            if (_fieldHeros.TryGetValue(tile, out var hero))
            {
                if (_clickedHero == hero)
                {
                    OnSelectHero?.Invoke(_clickedHero);
                }
                else
                {
                    var result = _overlapProcessor.OverlapHero(_clickedHero, hero);
                    Debug.Log(result);

                    switch (result)
                    {
                        case EHeroOverlapResult.None:
                            var startTile = _clickedHero.OccupiedTile;
                            var endTile = hero.OccupiedTile;

                            _fieldHeros.Remove(startTile);
                            _fieldHeros.Remove(endTile);

                            _fieldHero.Remove((endTile.X, endTile.Y));
                            _fieldHero.Remove((startTile.X, startTile.Y));

                            _fieldHeros.Add(endTile, _clickedHero);
                            _fieldHeros.Add(startTile, hero);

                            _fieldHero.Add((endTile.X, endTile.Y), _clickedHero);
                            _fieldHero.Add((startTile.X, startTile.Y), hero);

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
                            int evolution = _clickedHero.EvolutionLevel;
                            _heroSpawnService.SpawnHeroAtTile(uid, evolution, tile);

                            Debug.Log("합췌!!");
                            break;
                    }
                }
            }
            else
            {
                SetHeroPosition(tile, _clickedHero);
            }

            _markerPresenter.HideTileMarker();

            _clickedHero = null;
        }
        public void DragTile(Tile tile)
        {
            if (_clickedHero == null) return;

            _markerPresenter.UpdateTileMarker(tile);
        }

        public void SellHero(Hero hero)
        {
            Debug.Log("팔았다!");
            //TODO 판매 금액 산정 방법
            _gameGoldService.GainMoney(10);
        }

        public List<Hero> GetNearHeros(IReadOnlyTile pivot, int range)
        {
            List<Hero> heros = new List<Hero>();

            if (_fieldHeros.TryGetValue(pivot, out Hero pivotHero))
            {
                heros.Add(pivotHero);
            }

            if (range >= 1)
            {
                int[,] dir = new int[4, 2] { { -1, 0 }, { 0, 1 }, { 1, 0 }, { 0, -1 } };

                for (int i = 0; i < dir.GetLength(0); i++)
                {
                    int dx = pivot.X + dir[i, 0];
                    int dy = pivot.X + dir[i, 1];

                    if(_fieldHero.TryGetValue((dx,dy), out Hero hero))
                    {
                        heros.Add(hero);
                    }
                }
            }

            if(range >= 2)
            {
                int[,] dir = new int[4, 2] { { -1, -1 }, { -1, 1 }, { 1, 1 }, { 1, -1 } };

                for (int i = 0; i < dir.GetLength(0); i++)
                {
                    int dx = pivot.X + dir[i, 0];
                    int dy = pivot.X + dir[i, 1];

                    if (_fieldHero.TryGetValue((dx, dy), out Hero hero))
                    {
                        heros.Add(hero);
                    }
                }
            }

            return heros;
        }

        public void SetHeroBuff(EHeroStatType stat, float value)
        {
            throw new NotImplementedException();
        }
    }
}
