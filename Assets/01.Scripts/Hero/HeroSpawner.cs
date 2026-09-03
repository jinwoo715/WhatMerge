using WhatMerge.Heros;
using WhatMerge.Map;
using Skill;
using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Heros;

//영웅 정보
//공격력 정보
//스킬 정보

public class HeroSpawner : MonoBehaviour, IHeroSummonService, IHeroSkillConfigurator
{
    private readonly Dictionary<int, SkillSetContainer> _skillSets = new();
    private readonly HashSet<Hero> _activeHeroes = new();

    public SkillFactory factory;

    [SerializeField] private Hero _heroPrefab;
    private ObjectPool<Hero> _heroPool = new ObjectPool<Hero>();

    IFieldTileService _heroMapService;
    IResourcesReader _spriteAtlasRepository;
    IHeroInfoRepository _heroDataRepo;
    private IFatalStopService _fatalStop;
    private int _maxHeroLevel;

    private HeroDeck _heroDeck;

    public event Action<Tile, Hero> OnSpawndRanHero;

    public int SpawnedCount => _spawnedCount;
    public IReadOnlyCollection<Hero> ActiveHeroes => _activeHeroes;

    private int _spawnIndex = 0;
    private int _spawnedCount = 0;

    public void Init(
        IFieldTileService heroMapService,
        IResourcesReader spriteAtlasRepository,
        IHeroInfoRepository heroDataRepo,
        HeroDeck deck,
        int maxHeroLevel,
        IFatalStopService fatalStop)
    {
        _heroMapService = heroMapService;
        _spriteAtlasRepository = spriteAtlasRepository;
        _heroDataRepo = heroDataRepo;
        _heroDeck = deck;
        _maxHeroLevel = maxHeroLevel > 0
            ? maxHeroLevel
            : throw new ArgumentOutOfRangeException(nameof(maxHeroLevel));
        _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));

        RegisterSkillSets();
        _spawnIndex = 0;
        _spawnedCount = 0;
        _activeHeroes.Clear();
        _heroPool.Init(this.transform, _heroPrefab, 10);
    }

    public void RegisterSkillSets()
    {

        if (sets == null)
            throw new InvalidOperationException("HeroSpawner skill sets are not assigned.");

        _skillSets.Clear();

        for (int i = 0; i < sets.Count; i++)
        {
            SkillSetContainer set = sets[i];

            if (set == null)
                throw new InvalidOperationException($"HeroSpawner skill set at index {i} is null or missing.");

            if (_skillSets.ContainsKey(set.UID))
                throw new InvalidOperationException($"Duplicate skill set UID: {set.UID}.");

            HeroData heroData = GetRequiredHeroData(set.UID);
            SkillSetValidator.ValidateOrThrow(set, heroData, _maxHeroLevel);
            _skillSets.Add(set.UID, set);
        }
    }

    [Header("Mock")]
    public List<SkillSetContainer> sets;
    public bool IsSelectHeroSpawn;
    public int SpawnHeroUID;
    [Min(1)] public int HeroLevel;

    public bool TrySpawnRandomHero()
    {
        if (_fatalStop.IsFatalStopped)
            return false;

        int heroUid = 0;

        if (IsSelectHeroSpawn)
        {
            heroUid = SpawnHeroUID;
        }
        else
        {
            heroUid = _heroDeck.RanHeroUID();
        }

        bool isSpawned = SpawnHero(heroUid, 0);

        if (isSpawned)
            _spawnedCount++;

        return isSpawned;
    }

    public bool SpawnHero(int uid, int evolution)
    {
        if (_fatalStop.IsFatalStopped)
            return false;

        if (_heroMapService.TryGetNextFieldTile(out Tile tile))
        {
            SpawnHeroAtTile(uid, evolution, tile);
            return true;
        }
        return false;
    }

    public bool TrySpawnHero(int uid, int evolutionLevel)
    {
        return SpawnHero(uid, evolutionLevel);
    }

    public bool CanSpawnHero(int uid)
    {
        return !_fatalStop.IsFatalStopped
            && _heroDataRepo.TryGetHeroSaveData(uid, out _);
    }

    public void SpawnHeroAtTile(int uid, int evolutionLevel, Tile tile)
    {
        if (_fatalStop.IsFatalStopped)
            return;
        if (tile == null)
            throw new ArgumentNullException(nameof(tile));

        Vector3 pos = _heroMapService.GetTileWorldPosition(tile);
        Hero hero = null;
        bool tileOccupied = false;
        bool fieldRegistrationStarted = false;

        try
        {
            hero = CreateHero(uid, evolutionLevel, tile, pos);
            _heroMapService.OccupyFieldTile(tile);
            tileOccupied = true;

            fieldRegistrationStarted = true;
            OnSpawndRanHero?.Invoke(tile, hero);
        }
        catch (Exception exception)
        {
            if (!fieldRegistrationStarted)
            {
                try
                {
                    if (tileOccupied)
                        _heroMapService.FreeFieldTile(tile);
                }
                catch (Exception cleanupException)
                {
                    Debug.LogException(cleanupException);
                }

                if (hero != null && _activeHeroes.Contains(hero))
                {
                    try
                    {
                        ReturnHero(hero);
                    }
                    catch (Exception cleanupException)
                    {
                        Debug.LogException(cleanupException);
                    }
                }
            }

            _fatalStop.FatalStop(
                exception,
                $"Hero spawn failed. UID:{uid}, Evolution:{evolutionLevel}.");
            throw;
        }
    }

    private Hero CreateHero(int heroUid, int evolutionLevel, Tile tile, Vector3 spawnPos)
    {
        Hero hero = _heroPool.GetItem(spawnPos);
        _activeHeroes.Add(hero);

        try
        {
            hero.SetTile(tile, spawnPos);

            if (!_heroDataRepo.TryGetHeroSaveData(heroUid, out HeroSaveData saveData))
            {
                throw new InvalidOperationException(
                    $"Hero save data does not exist. Hero UID: {heroUid}.");
            }

            int heroLevel = IsSelectHeroSpawn ? HeroLevel : saveData.Level;
            if (heroLevel < 1 || heroLevel > _maxHeroLevel)
            {
                throw new InvalidOperationException(
                    $"Hero UID {heroUid} level {heroLevel} is outside 1-{_maxHeroLevel}.");
            }

            HeroData data = GetRequiredHeroData(heroUid);
            SpriteAtlas heroAtlas = GetRequiredHeroAtlas(data);

            hero.SpriteChanger.Init(heroAtlas, data.SpriteKey);
            hero.SetData(data, heroLevel, evolutionLevel, _spawnIndex++);

            SkillController controller = CreateSkillController(hero, hero.CurrentGrade);
            hero.AttachSkillController(controller);
            return hero;
        }
        catch
        {
            try
            {
                ReturnHero(hero);
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }

            throw;
        }
    }

    public SkillController CreateSkillController(Hero hero, HeroGrade targetGrade)
    {
        if (hero == null)
            throw new ArgumentNullException(nameof(hero));
        if (!_skillSets.TryGetValue(hero.UID, out SkillSetContainer skillSetContainer))
            throw new InvalidOperationException($"Skill set for hero UID {hero.UID} is not registered.");

        try
        {
            SkillSet skillSet = factory.CreateSkill(
                hero,
                targetGrade,
                hero.Level,
                skillSetContainer);

            try
            {
                return new SkillController(
                    skillSet.ActiveSkills,
                    skillSet.PassiveSkills,
                    hero,
                    StatCalculator.AS(GetRequiredHeroData(hero.UID).AttackSpeed),
                    _fatalStop);
            }
            catch
            {
                for (int i = 0; i < skillSet.ActiveSkills.Count; i++)
                {
                    try
                    {
                        skillSet.ActiveSkills[i]?.Dispose();
                    }
                    catch (Exception cleanupException)
                    {
                        Debug.LogException(cleanupException);
                    }
                }

                throw;
            }
        }
        catch (Exception exception)
        {
            exception.Data["HeroUID"] = hero.UID;
            exception.Data["Grade"] = targetGrade;
            exception.Data["Level"] = hero.Level;
            exception.Data["SkillSet"] = skillSetContainer.name;
            throw;
        }
    }

    public void ValidateHeroDefinition(int heroUid)
    {
        HeroData data = GetRequiredHeroData(heroUid);
        SpriteAtlas atlas = GetRequiredHeroAtlas(data);

        if (!_skillSets.ContainsKey(heroUid))
        {
            Debug.Log($"Skill set for hero UID {heroUid} is not registered.");
            return;
        }

        for (int evolutionLevel = 0; evolutionLevel <= 2; evolutionLevel++)
        {
            string spriteName = $"{data.SpriteKey}_{evolutionLevel + 1}_Idle";
            if (atlas.GetSprite(spriteName) == null)
            {
                throw new InvalidOperationException(
                    $"Sprite '{spriteName}' for hero UID {heroUid} is not registered.");
            }
        }
    }

    private HeroData GetRequiredHeroData(int heroUid)
    {
        return _heroDataRepo.GetHeroData(heroUid)
            ?? throw new InvalidOperationException($"Hero data for UID {heroUid} is not registered.");
    }

    private SpriteAtlas GetRequiredHeroAtlas(HeroData data)
    {
        return _spriteAtlasRepository.GetAtlas(data.SpriteKey)
            ?? throw new InvalidOperationException($"Sprite atlas '{data.SpriteKey}' is not registered.");
    }

    public void ReturnHero(Hero hero)
    {
        if (hero == null || !_activeHeroes.Remove(hero))
            return;

        _heroPool.ReturnItem(hero);
    }
}
