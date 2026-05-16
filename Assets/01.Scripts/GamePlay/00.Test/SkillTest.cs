using Combat;
using Entity;
using Newtonsoft.Json;
using Skill;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTest : MonoBehaviour
{
    [Header("Hero")]
    public int HeroUID;
    public int HeroLevel;
    
    public Hero hero;

    public SkillRepositoryTest skillRepository;

    private IServiceLocator _skillContext = new SkillServiceLocate();
    private IServiceLocator _ownerContext = new SkillServiceLocate();

    private IAttackRegister attackRegister = new BattleManager();

    private IFieldHeroService fieldHeroService;
    public HeroSkillFactoryTest factory = new HeroSkillFactoryTest();
    
    private ActiveSkillFactory _activeSkillFactory;
   

    public TextAsset activeSkill;

    [ContextMenu("Test")]
    public void SetData()
    {
        skillRepository.SetData(activeSkill);

        _skillContext.Register(attackRegister);
        _ownerContext.Register(hero);
        //_ownerContext.Register(hero.Transform);

        factory.Init(_skillContext, skillRepository);

        var upgradeData = skillRepository.GetHeroUpgradeSkillData(0);
        var skillDatas = upgradeData.GetSkills(HeroLevel);
        
        var skills = factory.CreateSkills(skillDatas, _ownerContext, hero);

        foreach (var skill in skills)
        {
            Debug.Log(skill);
        }
    }
}

[System.Serializable]
public class SkillRepositoryTest : ISkillDataRepository
{
    public HeroUpgradeSkillData upgradeSkillData;
    public BuffData buffSkillData;
    public DeBuffData deBuffData;
    public ExtraEffectData effectData;
    public SkillStatModifyData skillStatModifyData;

    Dictionary<int, ActiveSkillData> datas = new Dictionary<int, ActiveSkillData>();

    public void SetData(TextAsset tx)
    {
        datas.Clear();

        var data = JsonConvert.DeserializeObject<List<ActiveSkillData>>(tx.text);

        for (int i = 0; i < data.Count; i++)
        {
            datas.Add(data[i].UID, data[i]);
        }
    }

    public ActiveSkillData GetActiveSkillData(int uid)
    {
        return datas[uid];
    }

    public BuffData GetBuffData(int uid)
    {
        return buffSkillData;
    }

    public DeBuffData GetDeBuffData(int uid)
    {
        return deBuffData;
    }

    public ExtraEffectData GetExtraEffectData(int uid)
    {
        return effectData;
    }

    public HeroUpgradeSkillData GetHeroUpgradeSkillData(int uid)
    {
        return upgradeSkillData;
    }

    public SkillStatModifyData GetSkillModifierData(int uid)
    {
        return skillStatModifyData;
    }
}

//ภüป็

