using WhatMerge.Heros;
using WhatMerge.Map;
using Skill;
using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Heros;

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

    public int SpawnedCount => 0;

    private int _spawnIndex = 0;

    public void Init(IFieldTileService heroMapService, IResourcesReader spriteAtlasRepository, IHeroInfoRepository heroDataRepo, HeroDeck deck)
    {
        _heroMapService = heroMapService;
        _spriteAtlasRepository = spriteAtlasRepository;
        _heroDataRepo = heroDataRepo;
        _heroDeck = deck;

        _heroPool.Init(this.transform, _heroPrefab, 10);

        foreach (var set in sets)
        {
            dictionary.Add(set.UID, set);
        }
    }
    public bool TrySpawnRandomHero()
    {
        int heroUid = _heroDeck.RanHeroUID();

        return SpawnHero(heroUid, 0);
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
            Hero hero = SpawnHero(uid, evolution, pos);
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
        Hero hero = SpawnHero(uid, evolutionLevel, pos);
        hero.SetTile(tile, pos);

        _heroMapService.OccupyFieldTile(tile);

        OnSpawndRanHero?.Invoke(tile, hero);
    }

    public Hero SpawnHero(int heroUid, int evolutionLevel, Vector3 spawnPos)
    {
        Hero hero = _heroPool.GetItem(spawnPos);

        HeroSaveData saveData = GameManager.Data.GetSaveHeroData(heroUid);

        HeroData data = _heroDataRepo.GetHeroData(heroUid);
        ATKData atkData = _heroDataRepo.GetATKData(data.ATKUID);
        SpriteAtlas heroAtlas = _spriteAtlasRepository.GetAtlas(data.Name);

        HeroSpriteController heroSpriteController = hero.GetComponent<HeroSpriteController>();

        hero.SetData(data, atkData, heroSpriteController, saveData.Level, evolutionLevel, _spawnIndex++);
        heroSpriteController.Init(heroAtlas, data.Name, saveData.Level);

        var skillSet = factory.CreateSkill(hero, level, dictionary[hero.UID]);

        SkillController controller = new SkillController(skillSet.ActiveSkills, skillSet.PassiveSkills, hero, data.AS);

        hero.SetSkill(controller);
        return hero;
    }

    public void ReturnHero(Hero hero)
    {
        _heroPool.ReturnItem(hero);
    }
}
