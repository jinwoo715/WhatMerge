using Combat;
using Enemies;
using Entity;
using Skill.Data;
using Skill.Projectile;
using Skill.Summon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class SkillSet
    {
        public List<IActiveSkill> ActiveSkills = new List<IActiveSkill>();
        public List<IPassiveSkill> PassiveSkills = new List<IPassiveSkill>();
    }
    public class SkillCommonContext
    {
        public IProjectileProvider Projectile { get; }
        public ICombatService CombatService { get; }
        public IFieldHeroService FieldHeroService { get; }
        public IFieldEnemyService FieldEnemyService { get; }

        public SkillCommonContext(IProjectileProvider projectile, ICombatService combatService, IFieldHeroService fieldHeroService, IFieldEnemyService fieldEnemyService)
        {
            Projectile = projectile;
            CombatService = combatService;
            FieldEnemyService = fieldEnemyService;
            FieldHeroService = fieldHeroService;
        }
    }
    public class ActiveSkillContext
    {
        public Hero Hero { get; }
        public SkillAnimationData AnimationData { get; }
        public ExecutionSystemData System { get; }
        public ActiveSkillContext(Hero hero, SkillAnimationData animationData, ExecutionSystemData system)
        {
            Hero = hero;
            AnimationData = animationData;
            System = system;
        }
    }
    public class SkillFactory
    {
        private SkillCommonContext _skillExecutionService;
        public void Init(SkillCommonContext skillExecutionService)
        {
            _skillExecutionService = skillExecutionService;
        }

        public SkillSet CreateSkill(Hero owner, int level, SkillSetContainer set)
        {
            var sets = set.GetSets(level);

            SkillSet skillSet = new SkillSet();

            Dictionary<int, ISkillModifier> gets = new Dictionary<int, ISkillModifier>();

            Queue<EffectStatEnhancer> statEnhancers = new Queue<EffectStatEnhancer>();
            Queue<EffectStatEnhanceData> statEnhancerDatas = new Queue<EffectStatEnhanceData>();
            Queue<EffectChanceEnhancer> chanceEnhancers = new Queue<EffectChanceEnhancer>();
            Queue<ExtraEffect> effects = new Queue<ExtraEffect>();

            foreach (var data in sets)
            {
                var Skill = data.Skill;

                Debug.Log(Skill.UID);

                switch (Skill.SkillType)
                {
                    case ESkillType.Active:
                        ActiveSkillData so = Skill as ActiveSkillData;
                        ActiveSkill skill = CreateActiveSkill(so, owner);

                        skillSet.ActiveSkills.Add(skill);

                        gets.Add(so.UID, skill);

                        break;
                    case ESkillType.Passive:
                        
                        PassiveSkillData passiveSO = Skill as PassiveSkillData;
                        PassiveSkill passive = CreatePassiveSkill(passiveSO, owner);
                        passive.SetUID(passiveSO.UID);

                        skillSet.PassiveSkills.Add(passive);

                        break;

                    case ESkillType.SkillStatEnhancer:

                        EffectStatEnhanceData skillEnhancerData = Skill as EffectStatEnhanceData;
                        EffectStatEnhancer statEnhancer = new EffectStatEnhancer(skillEnhancerData);
                        statEnhancerDatas.Enqueue(skillEnhancerData);
                        statEnhancers.Enqueue(statEnhancer);

                        break;

                    case ESkillType.SkillChanceEnhancer:

                        EffectChanceEnhanceData skillChanceData = Skill as EffectChanceEnhanceData;
                        EffectChanceEnhancer statChanceEnhancer = new EffectChanceEnhancer(skillChanceData);
                        chanceEnhancers.Enqueue(statChanceEnhancer);

                        break;

                    case ESkillType.ExtraEffect:

                        ExtraEffectData entry = (Skill as ExtraEffectData);
                        ExtraEffect extraEffect = new ExtraEffect(entry);

                        effects.Enqueue(extraEffect);

                        break;
                }
            }

            foreach (var statAdder in statEnhancers)
            {
                if(gets.TryGetValue(statAdder.Data.UID, out ISkillModifier skill))
                {
                    statAdder.ApplySkill(skill);
                    Debug.Log("Stat Add");
                }
            }

            foreach (var chacneAdder in chanceEnhancers)
            {
                if (gets.TryGetValue(chacneAdder.Data.UID, out ISkillModifier skill))
                {
                    chacneAdder.ApplySkill(skill);
                    Debug.Log("Chance Add");
                }
            }
            foreach (var extra in effects)
            {
                if (gets.TryGetValue(extra.EffectEntry.TargetSkill.UID, out ISkillModifier skill))
                {
                    extra.ApplySkill(skill);
                    Debug.Log("Extra");
                }
            }
    

            return skillSet;
        }
        private ActiveSkill CreateActiveSkill(ActiveSkillData skillSO, Hero owner)
        {
            ITrigger trigger = GetTrigger(skillSO.Trigger);
            ITarget target = GetTarget(skillSO.Target, owner);

            ActiveSkillContext executionService = new ActiveSkillContext(owner, skillSO.AnimationData, skillSO.Execution);
            IExecute execution = GetExecution(executionService);
            
            ActiveSkill activeSkill = new ActiveSkill(skillSO.UID, owner, trigger, target, execution);

            return activeSkill;
        }
        public ITrigger GetTrigger(TriggerData system)
        {
            int value = system.RequireCost;
            switch (system.TriggerType)
            {
                case ESkillTriggerType.None:
                    return new NoneRequire();
                case ESkillTriggerType.HitCount:
                    return new HitCountRequire(value);
                case ESkillTriggerType.Mana:
                    return new ManaRequire(value);
            }
            return default;
        }
        private ITarget GetTarget(TargetData system, Hero owner)
        {
            switch (system.TargetType)
            {
                case ESkillTargetType.Self:
                    return new SelfTargetFinder(owner);
                case ESkillTargetType.NearHeros:
                    return new NearHeroFinder(_skillExecutionService.FieldHeroService, owner, system.Radius);
                case ESkillTargetType.AllHeros:
                    return new AllHeroFinder(_skillExecutionService.FieldHeroService);
                case ESkillTargetType.NearEnemies:
                    return new NearEnemyFinder(owner, system.Radius);
                case ESkillTargetType.AllEnemies:
                    return new AllEnemyFinder(_skillExecutionService.FieldEnemyService);
            }

            return null;
        }

        //TODO 스킬 생성 수정
        private IExecute GetExecution(ActiveSkillContext executionService)
        {
            string name = string.Empty;

            string skillName = executionService.System.name;
            if (skillName.Contains("TargetMelee"))
            {
                name = "Skill.TargetMeleeExecution";
            }
            else if (skillName.Contains("ConeMelee"))
            {
                name = "Skill.ConeMeleeExecution";
            }
            else if (skillName.Contains("Projectile"))
            {
                name = "Skill.TargetProjectile";
            }

            Debug.Log($"{skillName} / {name}");

            Type type = Type.GetType(name);

            Debug.Log(type);

            if (type != null)
            {
                object[] args = new object[] { executionService, _skillExecutionService };

               return (IExecute)Activator.CreateInstance(type, args);
                
            }
            else
            {
                return null;
            }
        }
        private PassiveSkill CreatePassiveSkill(PassiveSkillData passiveSkillSO, Hero owner)
        {
            var effects = passiveSkillSO.Effects;
            switch (passiveSkillSO.Target.TargetType)
            {
                case ESkillTargetType.Self:
                    return new SelfBuffPassive(owner, effects);
                case ESkillTargetType.NearHeros:
                    return new NearHeroBuffPassive(_skillExecutionService.FieldHeroService, owner, effects);
                case ESkillTargetType.AllHeros:
                    return new AllHeroBuffPassive(_skillExecutionService.FieldHeroService, owner, effects);
                case ESkillTargetType.NearEnemies:
                    break;
                case ESkillTargetType.AllEnemies:
                    break;
                default:
                    break;
            }
            return null;
        }
    }
}
     