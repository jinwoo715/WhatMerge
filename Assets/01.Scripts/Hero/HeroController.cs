using WhatMerge.Map;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Skill.Data;

namespace WhatMerge.Heros
{
    public class HeroController : IFieldHeroService, IFieldHeroSelecter, IHeroMergeExecutor, IDisposable
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
        private IHeroSkillConfigurator _skillConfigurator;
        private IFatalStopService _fatalStop;
        private bool _isBatching;
        private bool _batchFieldStateChanged;
        public event Action<Hero> OnSelectHero;
        public event Action OnChangedHeroPosition;
        public event Action OnFieldHeroesChanged;
        public event Action<Hero> OnSpawnedHero;
        public event Action<Hero> OnDestroyHero;
        public event Action<Hero> OnSellHeroEvent;

        public void Init(
            IHeroSummonService heroSpawnService,
            IHeroSkillConfigurator skillConfigurator,
            IHeroOverlapResult heroOverlapProcessor,
            IFieldTileService heroMapService,
            ITileIndicator markerPresenter,
            IGameGoldService gameGoldService,
            IFatalStopService fatalStop)
        {
            _heroSpawnService = heroSpawnService ?? throw new ArgumentNullException(nameof(heroSpawnService));
            _skillConfigurator = skillConfigurator ?? throw new ArgumentNullException(nameof(skillConfigurator));
            _gameGoldService = gameGoldService;
            _heroMapService = heroMapService;
            _overlapProcessor = heroOverlapProcessor;
            _markerPresenter = markerPresenter;
            _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));
        }
        public void ReturnHero(Hero hero)
        {
            if (IsCommandBlocked())
                return;

            ClearHero(hero);
        }
        public void ClearHero(Hero hero)
        {
            if (IsCommandBlocked())
                return;

            try
            {
                RemoveHeroInternal(hero);
                NotifyFieldStateChanged();
            }
            catch (Exception exception)
            {
                _fatalStop.FatalStop(exception, "Hero removal failed.");
                throw;
            }
        }
        public void SetHeroPosition(ITileReadOnly tile, Hero hero)
        {
            if (IsCommandBlocked())
                return;

            try
            {
                SetHeroPositionCore(tile, hero);
            }
            catch (Exception exception)
            {
                _fatalStop.FatalStop(exception, "Hero movement failed.");
                throw;
            }
        }

        private void SetHeroPositionCore(ITileReadOnly tile, Hero hero)
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
            InvokeFatal(OnChangedHeroPosition, "Hero position changed event failed.");
        }
        public void AddFieldHero(Tile tile, Hero hero)
        {
            _fieldHeros.Add(tile, hero);
            _fieldHero.Add((tile.X, tile.Y), hero);
            InvokeFatal(OnSpawnedHero, hero, "Hero spawned event failed.");
            NotifyFieldStateChanged();
        }
        public void PointDownTile(Tile tile)
        {
            if (IsCommandBlocked())
                return;

            if (_fieldHeros.TryGetValue(tile, out var hero))
            {
                _clickedHero = hero;
                _markerPresenter.ShowTileMarker(tile);
            }
        }
        public void PointUpTile(Tile tile)
        {
            if (IsCommandBlocked())
                return;

            try
            {
                PointUpTileCore(tile);
            }
            catch (Exception exception)
            {
                _fatalStop.FatalStop(exception, "Hero drag completion failed.");
                throw;
            }
            finally
            {
                _markerPresenter.HideTileMarker();
                _clickedHero = null;
            }
        }

        private void PointUpTileCore(Tile tile)
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
                            InvokeFatal(OnChangedHeroPosition, "Hero swap event failed.");

                            break;
                        case EHeroOverlapResult.Evolution:
                            EvolveHero(_clickedHero, hero);

                            break;
                        case EHeroOverlapResult.Merge:
                            int uid = _overlapProcessor.GetMergeHeroUID(_clickedHero.UID, hero.UID);
                            int evolution = _clickedHero.EvolutionLevel;

                            TryMergeHeroes(new[] { _clickedHero, hero }, uid, evolution);
                            break;
                    }
                }
            }
            else
            {
                SetHeroPosition(tile, _clickedHero);
            }

        }
        public void DragTile(Tile tile)
        {
            if (IsCommandBlocked())
                return;

            if (_clickedHero == null) return;

            _markerPresenter.UpdateTileMarker(tile);
        }
        public void SellHero(Hero hero)
        {
            if (IsCommandBlocked())
                return;

            ClearHero(hero);
            _gameGoldService.GainMoney(10);
            OnSellHeroEvent?.Invoke(hero);
        }

        private void EvolveHero(Hero material, Hero survivor)
        {
            if (material == null || survivor == null)
                throw new ArgumentNullException(material == null ? nameof(material) : nameof(survivor));
            if (!IsRegisteredHero(material) || !IsRegisteredHero(survivor))
                throw new InvalidOperationException("Evolution heroes must be registered on the field.");
            if (material.UID != survivor.UID
                || material.EvolutionLevel != survivor.EvolutionLevel)
            {
                throw new InvalidOperationException("Evolution heroes must have the same UID and evolution level.");
            }
            if (survivor.EvolutionLevel >= 2)
                throw new InvalidOperationException("Hero is already at the maximum evolution level.");

            HeroGrade targetGrade = (HeroGrade)((int)survivor.CurrentGrade + 1);
            Skill.SkillController nextController = null;
            bool ownershipTransferred = false;

            try
            {
                nextController = _skillConfigurator.CreateSkillController(survivor, targetGrade);
                BeginBatch();
                RemoveHeroInternal(material);
                survivor.UpgradeEvolution(nextController);
                ownershipTransferred = true;
                NotifyFieldStateChanged();
                CommitBatch();
            }
            catch (Exception exception)
            {
                AbortBatch();

                if (!ownershipTransferred)
                {
                    try
                    {
                        nextController?.Dispose();
                    }
                    catch (Exception cleanupException)
                    {
                        Debug.LogException(cleanupException);
                    }
                }

                _fatalStop.FatalStop(
                    exception,
                    $"Hero evolution failed. UID:{survivor.UID}, TargetGrade:{targetGrade}.");
                throw;
            }
        }

        public bool TryMergeHeroes(
            IReadOnlyList<Hero> materials,
            int resultHeroUID,
            int evolutionLevel)
        {
            if (IsCommandBlocked())
                return false;
            if (materials == null)
                throw new ArgumentNullException(nameof(materials));
            if (materials.Count == 0)
                throw new ArgumentException("Merge materials cannot be empty.", nameof(materials));
            if (resultHeroUID <= 0)
                throw new ArgumentOutOfRangeException(nameof(resultHeroUID));
            if (!_heroSpawnService.CanSpawnHero(resultHeroUID))
                return false;

            HashSet<Hero> uniqueMaterials = new();
            Hero spawnTileOwner = null;

            for (int i = 0; i < materials.Count; i++)
            {
                Hero material = materials[i]
                    ?? throw new ArgumentException($"Merge material at index {i} is null.", nameof(materials));

                if (!uniqueMaterials.Add(material))
                    throw new ArgumentException("Merge materials contain the same hero more than once.", nameof(materials));

                if (material.EvolutionLevel != evolutionLevel || !IsRegisteredHero(material))
                    return false;

                if (spawnTileOwner == null || material.SpawnIndex < spawnTileOwner.SpawnIndex)
                    spawnTileOwner = material;
            }

            Tile spawnTile = spawnTileOwner.OccupiedTile as Tile
                ?? throw new InvalidOperationException("The selected merge spawn tile is not a Tile.");

            try
            {
                BeginBatch();

                for (int i = 0; i < materials.Count; i++)
                    RemoveHeroInternal(materials[i]);

                _heroSpawnService.SpawnHeroAtTile(resultHeroUID, evolutionLevel, spawnTile);
                NotifyFieldStateChanged();
                CommitBatch();
                return true;
            }
            catch (Exception exception)
            {
                AbortBatch();
                _fatalStop.FatalStop(
                    exception,
                    $"Hero merge failed. ResultUID:{resultHeroUID}, Evolution:{evolutionLevel}.");
                throw;
            }
        }

        private bool IsRegisteredHero(Hero hero)
        {
            ITileReadOnly tile = hero.OccupiedTile;
            return tile != null
                && _fieldHeros.TryGetValue(tile, out Hero tileHero)
                && ReferenceEquals(tileHero, hero)
                && _fieldHero.TryGetValue((tile.X, tile.Y), out Hero positionHero)
                && ReferenceEquals(positionHero, hero);
        }

        private void RemoveHeroInternal(Hero hero)
        {
            if (hero == null)
                throw new ArgumentNullException(nameof(hero));

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

            if (ReferenceEquals(_clickedHero, hero))
            {
                _clickedHero = null;
                _markerPresenter.HideTileMarker();
            }

            try
            {
                InvokeFatal(OnDestroyHero, hero, "Hero destroyed event failed.");
            }
            finally
            {
                _heroSpawnService.ReturnHero(hero);
            }
        }

        private void NotifyFieldStateChanged()
        {
            if (_isBatching)
            {
                _batchFieldStateChanged = true;
                return;
            }

            InvokeFatal(OnChangedHeroPosition, "Hero position aggregate event failed.");
            InvokeFatal(OnFieldHeroesChanged, "Field heroes changed event failed.");
        }

        private void BeginBatch()
        {
            if (_isBatching)
                throw new InvalidOperationException("Nested hero field batches are not supported.");

            _isBatching = true;
            _batchFieldStateChanged = false;
        }

        private void CommitBatch()
        {
            if (!_isBatching)
                throw new InvalidOperationException("No hero field batch is active.");

            bool changed = _batchFieldStateChanged;
            _isBatching = false;
            _batchFieldStateChanged = false;

            if (!changed)
                return;

            InvokeFatal(OnChangedHeroPosition, "Hero position batch event failed.");
            InvokeFatal(OnFieldHeroesChanged, "Field heroes batch event failed.");
        }

        private void AbortBatch()
        {
            _isBatching = false;
            _batchFieldStateChanged = false;
        }

        private bool IsCommandBlocked()
        {
            return _fatalStop != null && _fatalStop.IsFatalStopped;
        }

        private void InvokeFatal(Action action, string context)
        {
            if (action == null)
                return;

            try
            {
                action.Invoke();
            }
            catch (Exception exception)
            {
                _fatalStop.FatalStop(exception, context);
                throw;
            }
        }

        private void InvokeFatal<T>(Action<T> action, T value, string context)
        {
            if (action == null)
                return;

            try
            {
                action.Invoke(value);
            }
            catch (Exception exception)
            {
                _fatalStop.FatalStop(exception, context);
                throw;
            }
        }

        public void CleanupSceneHeroes(IEnumerable<Hero> additionalHeroes)
        {
            HashSet<Hero> heroes = new HashSet<Hero>(_fieldHeros.Values);
            if (additionalHeroes != null)
                heroes.UnionWith(additionalHeroes.Where(hero => hero != null));

            foreach (Hero hero in heroes)
            {
                try
                {
                    hero.DisposeSkillController();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void Dispose()
        {
            _clickedHero = null;
            _fieldHeros.Clear();
            _fieldHero.Clear();
            OnSelectHero = null;
            OnChangedHeroPosition = null;
            OnFieldHeroesChanged = null;
            OnSpawnedHero = null;
            OnDestroyHero = null;
            OnSellHeroEvent = null;
        }
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
