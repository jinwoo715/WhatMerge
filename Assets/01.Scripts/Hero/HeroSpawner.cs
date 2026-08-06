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
    [Header("Mock")]
    public List<SkillSetContainer> sets;

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
    public bool TrySpawnRandomHero()
    {
        int heroUid = _heroDeck.RanHeroUID();

        bool isSpawned = SpawnHero(heroUid, 0);

        if (isSpawned)
            _spawnedCount++;

        return isSpawned;
    }

    public int uid;
    public int evolution;
    public int level;
    [ContextMenu("Spawn")]
    public void SpawnHeroTest()
    {
        SpawnHero(uid, evolution);
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

        hero.SetTile(tile, spawnPos);

        HeroSaveData saveData = GameManager.Data.GetSaveHeroData(heroUid);

        HeroData data = _heroDataRepo.GetHeroData(heroUid);
        ATKData atkData = _heroDataRepo.GetATKData(data.ATKUID);
        SpriteAtlas heroAtlas = _spriteAtlasRepository.GetAtlas(data.Name);

        HeroSpriteController heroSpriteController = hero.GetComponent<HeroSpriteController>();

        hero.SetData(data, atkData, heroSpriteController, saveData.Level, evolutionLevel, _spawnIndex++);
        heroSpriteController.Init(heroAtlas, data.Name, saveData.Level);

        if (!dictionary.TryGetValue(hero.UID, out SkillSetContainer skillSetContainer))
            throw new InvalidOperationException($"Skill set for hero UID {hero.UID} is not registered.");

        var skillSet = factory.CreateSkill(hero, saveData.Level, skillSetContainer);

        SkillController controller = new SkillController(
            skillSet.ActiveSkills,
            skillSet.PassiveSkills,
            hero,
            StatCalculator.AS(data.AS));

        hero.SetSkill(controller);
        return hero;
    }

    public void ReturnHero(Hero hero)
    {
        _heroPool.ReturnItem(hero);
    }
}
