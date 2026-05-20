using Entity;
using Heros;
using Map;
using Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class HeroSpawner : MonoBehaviour, IHeroSummonService
{
    [Header("Mock")]
    public Skill.Data.HeroUpgradeSkillSet set;
    public Skill.Data.SkillFactory factory;

    [SerializeField] private Hero _heroPrefab;
    private ObjectPool<Hero> _heroPool = new ObjectPool<Hero>();

    IHeroMapService _heroMapService;
    IResourcesReader _spriteAtlasRepository;
    ISkillFactory _skillCreater;
    IHeroInfoRepository _heroDataRepo;
    ISkillDataRepository _skillDataReader;

    private HeroDeck _heroDeck;

    public event Action<Tile, Hero> OnSpawndRanHero;

    public int SpawnedCount => 0;

    public void Init(IHeroMapService heroMapService, ISkillFactory skillCreater, IResourcesReader spriteAtlasRepository, IHeroInfoRepository heroDataRepo, HeroDeck deck)
    {
        _heroMapService = heroMapService;
        _spriteAtlasRepository = spriteAtlasRepository;
        _skillCreater = skillCreater;
        _heroDataRepo = heroDataRepo;
        _heroDeck = deck;

        _heroPool.OnCreateEvent += SpawnInit;
        _heroPool.Init(this.transform, _heroPrefab, 10);
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
        if (_heroMapService.TryGetNextHeroTile(out Tile tile))
        {
            Vector3 pos = _heroMapService.GetTileWorldPosition(tile);
            Hero hero = SpawnHero(uid, evolution, pos);
            hero.SetTile(tile, pos);

            _heroMapService.OccupyHeroTile(tile);

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

        _heroMapService.OccupyHeroTile(tile);

        OnSpawndRanHero?.Invoke(tile, hero);
    }

    public Hero SpawnHero(int heroUid, int evolutionLevel, Vector3 spawnPos)
    {
        Hero hero = _heroPool.GetItem(spawnPos);

        HeroSaveData saveData = GameManager.Data.GetSaveHeroData(heroUid);

        HeroData data = _heroDataRepo.GetHeroData(heroUid);
        ATKData atkData = _heroDataRepo.GetATKData(data.ATKUID);
        SpriteAtlas heroAtlas = _spriteAtlasRepository.GetAtlas(data.Name);

        hero.SetData(data, atkData, heroAtlas, saveData.Level);
        hero.SetEvolution(evolutionLevel);

        Debug.Log("Start");

        var skillSet = factory.CreateSkill(hero, level, set);

        Debug.Log(skillSet.ActiveSkills.Count);
        Debug.Log(skillSet.PassiveSkills.Count);

        Skill.Data.SkillController controller = new Skill.Data.SkillController(skillSet.ActiveSkills, skillSet.PassiveSkills, hero, data.AS);

        //HeroSkillBundle skillBundle = new HeroSkillBundle(data.BaseAttack, data.FirstSkill, data.SecondSkill, data.SpecialSkill);
        //List<IActiveSkill> skills = _skillCreater.CreateActiveSkill(skillBundle, hero.Context);
        //hero.SetSkill(skills);
        hero.skillController = controller;
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


}
