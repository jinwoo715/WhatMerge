using Entity;
using Heros;
using Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;


public interface IHeroInfoRepository
{
    HeroData GetHeroData(int uid);
    ATKData GetATKData(int heroUid);
    HeroSaveData GetHeroSaveData(int uid);
}

public class HeroSpawner : MonoBehaviour, IHeroSummonService
{
    [SerializeField] private Hero _heroPrefab;
    private ObjectPool<Hero> _heroPool = new ObjectPool<Hero>();

    IHeroMapService _heroMapService;
    IResourcesReader _spriteAtlasRepository;
    ISkillCreater _skillCreater;
    IHeroInfoRepository _heroDataRepo;

    private HeroDeck _heroDeck;

    public event Action<Tile, Hero> OnSpawndRanHero;

    public int SpawnedCount => 0;

    public void Init(IHeroMapService heroMapService, ISkillCreater skillCreater, IResourcesReader spriteAtlasRepository, IHeroInfoRepository heroDataRepo, HeroDeck deck)
    {
        _heroMapService = heroMapService;
        _spriteAtlasRepository = spriteAtlasRepository;
        _skillCreater = skillCreater;
        _heroDataRepo = heroDataRepo;
        _heroDeck = deck;

        _heroPool.OnCreateEvent += SpawnInit;
        _heroPool.Init(this.transform, _heroPrefab, 10);
    }

    public Hero SpawnHero(int heroUid, Vector3 spawnPos, int evolutionLevel)
    {
        Hero hero = _heroPool.GetItem(spawnPos);

        HeroSaveData saveData = GameManager.Data.GetSaveHeroData(heroUid);

        HeroData data = _heroDataRepo.GetHeroData(heroUid);
        ATKData atkData = _heroDataRepo.GetATKData(data.ATKUID);
        SpriteAtlas heroAtlas = _spriteAtlasRepository.GetAtlas(data.Name);

        hero.SetData(data, atkData, heroAtlas, saveData.Level);
        hero.SetEvolution(evolutionLevel);

        HeroSkillBundle skillBundle = new HeroSkillBundle(data.BaseAttack, data.FirstSkill, data.SecondSkill, data.SpecialSkill);
        List<ISkill> skills = _skillCreater.CreateActiveSkill(skillBundle, hero.Context);

        hero.SetSkill(skills);

        return hero;
    }

    public void SpawnInit(Hero hero)
    {
        hero.SpawnInit();
    }
    public void ReturnHero(Hero hero)
    {
        _heroPool.ReturnItem(hero);
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
        Hero hero = SpawnHero(uid, pos, 0);
        hero.SetTile(tile, pos);

        _heroMapService.OccupyHeroTile(tile);
    }
}
