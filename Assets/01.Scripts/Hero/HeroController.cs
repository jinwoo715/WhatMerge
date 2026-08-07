using Combat;
using WhatMerge.Map;
using Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.U2D;
using WhatMerge.Heros;
using System.Linq;
using Skill.Data;

namespace Combat { }

namespace WhatMerge.Heros
{
    public interface IHeroSummonService
    {
        int SpawnedCount { get; }
        bool TrySpawnRandomHero();
        bool TrySpawnHero(int uid, int evolutionLevel);
        void SpawnHeroAtTile(int uid, int evolutionLevel, Tile tile);
        void ReturnHero(Hero hero);

        event Action<Tile, Hero> OnSpawndRanHero;
    }

    public interface IFieldHeroSelecter
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
            if (first.UID == second.UID && first.EvolutionLevel < 2)
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

    public class HeroController : IFieldHeroService, IFieldHeroSelecter
    {
        private Dictionary<ITileReadOnly, Hero> _fieldHeros = new Dictionary<ITileReadOnly, Hero>();
        private Dictionary<(int x,int y), Hero> _fieldHero = new Dictionary<(int, int), Hero>();

        private Hero _clickedHero = null;

        public IReadOnlyList<Hero> GetAllFieldHero => _fieldHeros.Values.ToList();
        public bool IsUsableBag => CurrentUsedBagItem < TotalBagItem;
        public int TotalBagItem => 3;
        public int CurrentUsedBagItem => 0;

        private IFieldTileService _heroMapService;
        private IHeroOverlapResult _overlapProcessor;
        private ITileIndicator _markerPresenter;
        private IGameGoldService _gameGoldService;
        private IHeroSummonService _heroSpawnService;
        public event Action<Hero> OnSelectHero;
        public event Action OnChangedHeroPosition;
        public event Action<Hero> OnSpawnedHero;
        public event Action<Hero> OnDestroyHero;

        public void Init(IHeroSummonService heroSpawnService, IHeroOverlapResult heroOverlapProcessor, IFieldTileService heroMapService, ITileIndicator markerPresenter, IGameGoldService gameGoldService)
        {
            _heroSpawnService = heroSpawnService;
            _gameGoldService = gameGoldService;
            _heroMapService = heroMapService;
            _overlapProcessor = heroOverlapProcessor;
            _markerPresenter = markerPresenter;
        }
        public void ReturnHero(Hero hero)
        {
            ClearHero(hero);
        }

        public void ClearHero(Hero hero)
        {
            ITileReadOnly tile = hero.OccupiedTile;

            if (tile == null)
                throw new InvalidOperationException($"Hero '{hero.name}' has no occupied tile.");

            if (!_fieldHeros.TryGetValue(tile, out Hero tileHero) || !ReferenceEquals(tileHero, hero))
                throw new InvalidOperationException($"Hero '{hero.name}' is not registered at its occupied tile.");

            if (!_fieldHero.TryGetValue((tile.X, tile.Y), out Hero positionHero) || !ReferenceEquals(positionHero, hero))
                throw new InvalidOperationException($"Hero '{hero.name}' is not registered at ({tile.X}, {tile.Y}).");

            _fieldHeros.Remove(tile);
            _fieldHero.Remove((tile.X, tile.Y));
            _heroMapService.FreeFieldTile(tile);
            OnChangedHeroPosition?.Invoke();

            try
            {
                OnDestroyHero?.Invoke(hero);
            }
            finally
            {
                _heroSpawnService.ReturnHero(hero);
            }
        }

        public void SetHeroPosition(ITileReadOnly tile, Hero hero)
        {
            ITileReadOnly previousTile = hero.OccupiedTile;

            if (previousTile == null)
                throw new InvalidOperationException($"Hero '{hero.name}' has no occupied tile.");

            if (previousTile.X == tile.X && previousTile.Y == tile.Y)
                return;

            if (!_fieldHeros.TryGetValue(previousTile, out Hero tileHero) || !ReferenceEquals(tileHero, hero))
                throw new InvalidOperationException($"Hero '{hero.name}' is not registered at its occupied tile.");

            if (!_fieldHero.TryGetValue((previousTile.X, previousTile.Y), out Hero positionHero) || !ReferenceEquals(positionHero, hero))
                throw new InvalidOperationException(
                    $"Hero '{hero.name}' is not registered at ({previousTile.X}, {previousTile.Y}).");

            if (_fieldHeros.ContainsKey(tile) || _fieldHero.ContainsKey((tile.X, tile.Y)))
                throw new InvalidOperationException($"Destination tile ({tile.X}, {tile.Y}) is already occupied.");

            _heroMapService.FreeFieldTile(previousTile);
            _fieldHeros.Remove(previousTile);
            _fieldHero.Remove((previousTile.X, previousTile.Y));

            _heroMapService.OccupyFieldTile(tile);
            _fieldHeros.Add(tile, hero);
            _fieldHero.Add((tile.X, tile.Y), hero);

            hero.SetTile(tile, _heroMapService.GetTileWorldPosition(tile));
            OnChangedHeroPosition?.Invoke();
        }
        public void AddFieldHero(Tile tile, Hero hero)
        {
            _fieldHeros.Add(tile, hero);
            _fieldHero.Add((tile.X, tile.Y), hero);
            OnSpawnedHero?.Invoke(hero);
            OnChangedHeroPosition?.Invoke();
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
                            OnChangedHeroPosition?.Invoke();

                            break;
                        case EHeroOverlapResult.Evolution:
                            ReturnHero(_clickedHero);
                            hero.UpgradeEvolution();

                            break;
                        case EHeroOverlapResult.Merge:
                            int uid = _overlapProcessor.GetMergeHeroUID(_clickedHero.UID, hero.UID);
                            int evolution = _clickedHero.EvolutionLevel;

                            ReturnHero(_clickedHero);
                            ReturnHero(hero);

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
            ClearHero(hero);
            _gameGoldService.GainMoney(10);
        }

        //TODO 영웅 범위 탐색
        public List<Hero> GetNearHeros(ITileReadOnly pivot, HeroSearchType range)
        {
            List<Hero> heros = new List<Hero>();

            switch (range)
            {
                case HeroSearchType.Single:
                    if (_fieldHeros.TryGetValue(pivot, out Hero pivotHero))
                    {
                        heros.Add(pivotHero);
                    }
                    break;

                case HeroSearchType.Cross:

                    heros.AddRange(GetColHeros(pivot));
                    heros.AddRange(GetRowHeros(pivot));

                    break;
                case HeroSearchType.Surrounding:

                    heros.AddRange(GetSurroundHeros(pivot));

                    break;
                case HeroSearchType.All:

                    heros = _fieldHero.Values.ToList();

                    break;
            }

            return heros;
        }

        private List<Hero> GetRowHeros(ITileReadOnly pivot)
        {
            List<Hero> heros = new List<Hero>();
            for (int i = 0; i < _heroMapService.MaxRow; i++)
            {
                if(_fieldHero.TryGetValue((pivot.X, i), out var hero))
                {
                    heros.Add(hero);
                }
            }

            return heros;
        }
        private List<Hero> GetColHeros(ITileReadOnly pivot)
        {
            List<Hero> heros = new List<Hero>();
            for (int i = 0; i < _heroMapService.MaxCol; i++)
            {
                if (_fieldHero.TryGetValue((i, pivot.Y), out var hero))
                {
                    if (pivot.X == i)
                        continue;

                    heros.Add(hero);
                }
            }

            return heros;
        }
        private List<Hero> GetSurroundHeros(ITileReadOnly pivot)
        {
            List<Hero> heros = new List<Hero>();

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int x = pivot.X + i;
                    int y = pivot.Y + j;

                    if (x < 0 || x >= _heroMapService.MaxCol || y < 0 || y >= _heroMapService.MaxRow || (x == pivot.X && y == pivot.Y))
                        continue;

                    if(_fieldHero.TryGetValue((x,y), out var hero))
                    {
                        heros.Add(hero);
                    }
                }
            }

            return heros;
        }
    }
}
