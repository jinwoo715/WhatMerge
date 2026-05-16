using Enemies;
using Entity;
using Heros;
using Skill;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeroSkillSet
{
    public List<IActiveSkill> ActiveSkills = new List<IActiveSkill>();
    public List<IPassiveSkill> PassiveSkills = new List<IPassiveSkill>();
}





public class HeroSkillFactoryTest : ISkillFactory
{
    private IServiceLocator _skillContext;
    private ISkillDataRepository _skillRepository;

    private IFieldHeroService fieldHeroService;
    private IFieldEnemyService fieldEnemyService;

    private ActiveSkillFactory _activeSkillFactory = new ActiveSkillFactory();

    public HeroSkillSet CreateHeroSkillSet(List<string> skillNames, IServiceLocator ownerContext, Hero owner)
    {
        HeroSkillSet heroSkillSet = new HeroSkillSet();

        Dictionary<int, ISkill> AllSkills = new Dictionary<int, ISkill>();

        Stack<ISkillExtraEffecter> extraEffecter = new Stack<ISkillExtraEffecter>();
        Stack<SkillParamModifier> skillModifiers = new Stack<SkillParamModifier>();

        foreach (var item in skillNames)
        {
            string[] str = item.Split("-");
            int skillUid = int.Parse(str[1]);

            switch (str[0])
            {
                case "Active":
                    var activeSkill = _activeSkillFactory.CreateSkill(skillUid, ownerContext);
                    heroSkillSet.ActiveSkills.Add(activeSkill);
                    AllSkills.Add(skillUid, activeSkill);

                    break;

                case "Buff":

                    var data = _skillRepository.GetBuffData(skillUid);
                    var buff = CreateBuff(data);
                    buff.Init(fieldHeroService, owner, data);
                    heroSkillSet.PassiveSkills.Add(buff);
                    AllSkills.Add(skillUid, buff);

                    break;

                case "Debuff":

                    var debuffData = _skillRepository.GetDeBuffData(skillUid);
                    var debuff = new DebuffPassive(fieldEnemyService, debuffData);
                    heroSkillSet.PassiveSkills.Add(debuff);
                    AllSkills.Add(skillUid, debuff);

                    break;

                case "Extra":

                    var extraData = _skillRepository.GetExtraEffectData(skillUid);
                    var extraEffect = new SkillExtraEffecter(extraData);
                    extraEffecter.Push(extraEffect);

                    break;

                case "Upgrade":

                    var upgradeData = _skillRepository.GetSkillModifierData(skillUid);
                    var modifier = new SkillParamModifier(upgradeData);
                    skillModifiers.Push(modifier);
                    break;
            }
        }

        var activeSkillDic = heroSkillSet.ActiveSkills.ToDictionary(skill => skill.UID);

        while (extraEffecter.Count != 0)
        {
            var effecter = extraEffecter.Pop();

            if(activeSkillDic.TryGetValue(effecter.TargetSkillUID, out IActiveSkill skill))
            {
                skill.RegisterExtraEffect(effecter);
            }
        }

        while (skillModifiers.Count != 0)
        {
            var modifier = skillModifiers.Pop();

            if(AllSkills.TryGetValue(modifier.TargetUID, out ISkill target))
            {
                modifier.Excute(target);
            }
        }

        return heroSkillSet;
    }

    public void Init(IServiceLocator skillContext, ISkillDataRepository skillRepository)
    {
        _skillContext = skillContext;
        _skillRepository = skillRepository;
        
        _activeSkillFactory.Init(_skillContext, skillRepository);
    }
    public List<ISkill> CreateSkills(List<string> skillNames, IServiceLocator ownerContext, Hero owner)
    {
        List<ISkill> skiils = new List<ISkill>();
        List<ISkill> buffSkill = new List<ISkill>();
        List<IActiveSkill> activeSkills = new List<IActiveSkill>();
        Stack<ISkillExtraEffecter> extraEffecter = new Stack<ISkillExtraEffecter>();
        Stack<SkillParamModifier> skillModifiers = new Stack<SkillParamModifier>();

        foreach (var item in skillNames)
        {
            string[] str = item.Split("-");
            int skillUid = int.Parse(str[1]);

            switch (str[0])
            {
                case "Active":
                    var activeSkill = _activeSkillFactory.CreateSkill(skillUid, ownerContext);
                    activeSkills.Add(activeSkill);
                    skiils.Add(activeSkill);
                    break;

                case "Buff":
                    var data = _skillRepository.GetBuffData(skillUid);
                    var buff = CreateBuff(data);
                    buff.Init(fieldHeroService, owner, data);
                    skiils.Add(buff);
                    buffSkill.Add(buff);
                    break;

                case "Debuff":
                    var debuffData = _skillRepository.GetDeBuffData(skillUid);
                    var debuff = new DebuffPassive(fieldEnemyService, debuffData);
                    skiils.Add(debuff);
                    break;

                case "Extra":

                    var extraData = _skillRepository.GetExtraEffectData(skillUid);
                    var extraEffect = new SkillExtraEffecter(extraData);
                    extraEffecter.Push(extraEffect);

                    break;

                case "Upgrade":

                    var upgradeData = _skillRepository.GetSkillModifierData(skillUid);
                    var modifier = new SkillParamModifier(upgradeData);
                    skillModifiers.Push(modifier);
                    break;
            }
        }

        int count = 0;

        while(extraEffecter.Count != 0) 
        { 
            var effecter = extraEffecter.Pop();

            foreach (var skill in activeSkills)
            {
                if (skill.UID == effecter.TargetSkillUID)
                {
                    skill.RegisterExtraEffect(effecter);
                    break;
                }
            }

            count++;

            if (count > 50)
                break;
        }

        count = 0;

        while (skillModifiers.Count != 0)
        {
            var modifier = skillModifiers.Pop();

            foreach (var skill in activeSkills)
            {
                if (skill.UID == modifier.TargetUID)
                {
                    modifier.Excute(skill);
                    break;
                }
            }

            count++;

            if (count > 50)
                break;
        }

        owner.SetPassive(buffSkill);

        return skiils;
    }
    public List<IActiveSkill> CreateActiveSkill(HeroSkillBundle skillBundle, IServiceLocator ownerContext)
    {
        throw new System.NotImplementedException();
    }
    public BuffBase CreateBuff(BuffData buffData)
    {
        if (buffData.TargetType == EBuffTargetType.Self)
        {
            return new SelfBuff();
        }
        else if (buffData.TargetType == EBuffTargetType.NearHeros)
        {
            return new NearBuff();
        }
        else
            return new AllBuff();
    }
}