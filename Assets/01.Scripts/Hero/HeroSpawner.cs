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

public class HeroSpawner : MonoBehaviour, IHeroSummonService
{
    private Dictionary<int, SkillSetContainer> dictionary = new Dictionary<int, SkillSetContainer>();

    public SkillFactory factory;

    [SerializeField] private Hero _heroPrefab;
    private ObjectPool<Hero> _heroPool = new ObjectPool<Hero>();

    IFieldTileService _heroMapService;
    IResourcesReader _spriteAtlasRepository;
    IHeroInfoRepository _heroDataRepo;

    private HeroDeck _heroDeck;

    public event Action<Tile, Hero> OnSpawndRanHero;

    public int SpawnedCount => _spawnedCount;

    private int _spawnIndex = 0;
    private int _spawnedCount = 0;

    public void Init(IFieldTileService heroMapService, IResourcesReader spriteAtlasRepository, IHeroInfoRepository heroDataRepo, HeroDeck deck)
    {
        _heroMapService = heroMapService;
        _spriteAtlasRepository = spriteAtlasRepository;
        _heroDataRepo = heroDataRepo;
        _heroDeck = deck;

        if (sets == null)
            throw new InvalidOperationException("HeroSpawner skill sets are not assigned.");

        dictionary.Clear();
        _spawnIndex = 0;
        _spawnedCount = 0;

        for (int i = 0; i < sets.Count; i++)
        {
            SkillSetContainer set = sets[i];

            if (set == null)
                throw new InvalidOperationException($"HeroSpawner skill set at index {i} is null or missing.");

            if (dictionary.ContainsKey(set.UID))
                throw new InvalidOperationException($"Duplicate skill set UID: {set.UID}.");

            dictionary.Add(set.UID, set);
        }

        _heroPool.Init(this.transform, _heroPrefab, 10);
    }

    [Header("Mock")]
    public List<SkillSetContainer> sets;
    public bool IsSelectHeroSpawn;
    public int SpawnHeroUID;
    [Min(1)] public int HeroLevel;

    public bool TrySpawnRandomHero()
    {
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
        if (_heroMapService.TryGetNextFieldTile(out Tile tile))
        {
            Vector3 pos = _heroMapService.GetTileWorldPosition(tile);
            Hero hero = SpawnHero(uid, evolution, tile, pos);
            hero.SetTile(tile, pos);

            _heroMapService.OccupyFieldTile(tile);

            OnSpawndRanHero?.Invoke(tile, hero);

            return true;
        }
        return false;
    }

    public bool TrySpawnHero(int uid, int evolutionLevel)
    {
        return SpawnHero(uid, evolutionLevel);
    }

    public void SpawnHeroAtTile(int uid, int evolutionLevel, Tile tile)
    {
        Vector3 pos = _heroMapService.GetTileWorldPosition(tile);
        Hero hero = SpawnHero(uid, evolutionLevel, tile, pos);

        _heroMapService.OccupyFieldTile(tile);

        OnSpawndRanHero?.Invoke(tile, hero);
    }

    public Hero SpawnHero(int heroUid, int evolutionLevel, Tile tile, Vector3 spawnPos)
    {
        Hero hero = _heroPool.GetItem(spawnPos);

        try
        {
            hero.SetTile(tile, spawnPos);

            HeroSaveData saveData = GameManager.Data.GetSaveHeroData(heroUid);
            int heroLevel = IsSelectHeroSpawn ? HeroLevel : saveData.Level;

            HeroData data = GetRequiredHeroData(heroUid);
            SpriteAtlas heroAtlas = GetRequiredHeroAtlas(data);

            hero.SpriteChanger.Init(heroAtlas, data.SpriteKey);
            hero.SetData(data, heroLevel, evolutionLevel, _spawnIndex++);

            if (!dictionary.TryGetValue(hero.UID, out SkillSetContainer skillSetContainer))
                throw new InvalidOperationException($"Skill set for hero UID {hero.UID} is not registered.");

            var skillSet = factory.CreateSkill(hero, heroLevel, skillSetContainer);
            SkillController controller = new SkillController(
                skillSet.ActiveSkills,
                skillSet.PassiveSkills,
                hero,
                StatCalculator.AS(data.AttackSpeed));

            hero.SetSkill(controller);
            return hero;
        }
        catch
        {
            _heroPool.ReturnItem(hero);
            throw;
        }
    }

    public void ValidateHeroDefinition(int heroUid)
    {
        HeroData data = GetRequiredHeroData(heroUid);
        SpriteAtlas atlas = GetRequiredHeroAtlas(data);

        if (!dictionary.ContainsKey(heroUid))
            throw new InvalidOperationException($"Skill set for hero UID {heroUid} is not registered.");

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
        _heroPool.ReturnItem(hero);
    }
}
