using Entity;
using Heros;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class HeroSpawner : MonoBehaviour
{
    [SerializeField] private Hero _heroPrefab;
    private ObjectPool<Hero> _heroPool = new ObjectPool<Hero>();

    ISpriteAtlasRepository _spriteAtlasRepository;
    ISkillCreater _skillCreater;

    public void Init(ISkillCreater skillCreater, ISpriteAtlasRepository spriteAtlasRepository)
    {
        _spriteAtlasRepository = spriteAtlasRepository;
        _skillCreater = skillCreater;

        _heroPool.OnCreateEvent += SpawnInit;
        _heroPool.Init(this.transform, _heroPrefab, 10);
    }

    public Hero SpawnHero(int heroUid, Vector3 spawnPos, int evolutionLevel)
    {
        Hero hero = _heroPool.GetItem(spawnPos);

        HeroSaveData saveData = GameManager.Data.GetSaveHeroData(heroUid);



        HeroData data = GameManager.Data.GetHeroData(heroUid);
        ATKData atkData = GameManager.Data.GetATKData(data.ATKUID);
        SpriteAtlas heroAtlas = _spriteAtlasRepository.GetHeroSpriteAtlas(data.UID);

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
}
