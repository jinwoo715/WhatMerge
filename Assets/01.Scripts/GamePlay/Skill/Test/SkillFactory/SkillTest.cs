using Combat;
using Enemies;
using Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data 
{
    public class SkillController : ISkillResourceModifier
    {
        private List<IActiveSkill> _activeSkills;
        private List<IPassiveSkill> _passiveSkills;
        private MonoBehaviour _coroutineRunner;

        private float _executionTime;
        private float _time;
        private float _mana;
        private int _hitCount;

        private bool _isUsingSkill = false;

        private float _manaChargeMultiple = 1;

        public SkillController(List<IActiveSkill> activeSkills, List<IPassiveSkill> passiveSkills, MonoBehaviour coroutineRunner, float delay)
        {
            _activeSkills = activeSkills;
            _passiveSkills = passiveSkills;
            _coroutineRunner = coroutineRunner;
            _executionTime = delay;

            ApplyPassive();
        }

        public void ApplyPassive()
        {
            foreach (var passive in _passiveSkills)
            {
                passive.Apply();
            }
        }

        public void UpdateDelayTime(float delay)
        {
            _executionTime = delay;
        }

        public void Tick(float tickValue)
        {
            if (_isUsingSkill == true) return;

            _time += tickValue;
            ChargeMana(tickValue);

            if(_time >= _executionTime)
            {
                IActiveSkill executeSkill = GetUsableSkill();

                if (executeSkill != null)
                {
                    _coroutineRunner.StartCoroutine(CoExecuteSkill(executeSkill));
                }

                _time = 0;
            }
        }
        private IActiveSkill GetUsableSkill()
        {
            IActiveSkill usableSkill = null;

            SkillTriggerContext context = new SkillTriggerContext(_hitCount, _mana);

            int skillCount = _activeSkills.Count;
            for (int i = skillCount-1; i >= 0; i--)
            {
                IActiveSkill skill = _activeSkills[i];

                if (skill.IsUsable(context))
                {
                    usableSkill = skill;
                    break;
                }
            }

            return usableSkill;
        }

        private IEnumerator CoExecuteSkill(IActiveSkill skill)
        {
            _isUsingSkill = true;

            Debug.Log("Execute Skill");
            skill.Trigger.UseTriggerResource(this);
            yield return _coroutineRunner.StartCoroutine(skill.Execute());

            _isUsingSkill = false;
        }

        private void ChargeMana(float manaAmount)
        {
            _mana += manaAmount * 10 * _manaChargeMultiple;
        }

        public void ConsumeHitCount(int count)
        {
            Debug.Log("Concume HitCount");
            _hitCount = 0;
            //_hitCount -= count;
        }

        public void ConsumeMana(float amount)
        {
            Debug.Log("Concume Mana");
            _mana = 0;
            //_mana -= amount;
        }

        public void AddHitCount(int count)
        {
            _hitCount += count;
        }

        public void AddMana(float amount)
        {
            _mana += amount;
        }

        public void IncreaseManaAmoutRaio(float ratio)
        {
            _manaChargeMultiple += ratio;
        }
    }
    public struct SkillTriggerContext
    {
        public int HitCount;
        public float Mana;

        public SkillTriggerContext(int hitCount, float mana)
        {
            HitCount = hitCount;
            Mana = mana;
        }
    }
    public class SkillSet
    {
        public List<IActiveSkill> ActiveSkills = new List<IActiveSkill>();
        public List<IPassiveSkill> PassiveSkills = new List<IPassiveSkill>();
    }

    public class SkillExecutionService
    {
        public IProjectileProvider Projectile { get; }
        public ISummonProvider Summon { get; }
        public IVFXService VfxService { get; }
        public ICombatService CombatService { get; }
        public IFieldHeroService FieldHeroService { get; }
        public IFieldEnemyService FieldEnemyService { get; }
        public IBuffRegister BuffRegister { get; }
    }

    public class SkillFactory
    {
        private IVFXService _vfxService;
        private ICombatService _combatService;
        private IFieldHeroService _fieldHeroService;
        private IFieldEnemyService _fieldEnemyService;

        private SkillExecutionService _skillExecutionService;
        public void Init(SkillExecutionService skillExecutionService)
        {
            _skillExecutionService = skillExecutionService;
        }

        public void Init(IVFXService vfxService, ICombatService attackRegister, IFieldHeroService fieldHeroService, IFieldEnemyService fieldEnemyService)
        {
            _vfxService = vfxService;
            _combatService = attackRegister;
            _fieldHeroService = fieldHeroService;
            _fieldEnemyService = fieldEnemyService;
        }
        public SkillSet CreateSkill(Hero owner, int level, HeroUpgradeSkillSet set)
        {
            var sets = set.GetSets(level);

            Debug.Log(sets.Count);

            SkillSet skillSet = new SkillSet();

            Dictionary<int, ISkill> gets = new Dictionary<int, ISkill>();
            
            Queue<EffectStatEnhancer> statEnhancers = new Queue<EffectStatEnhancer>();
            Queue<EffectChanceEnhancer> chanceEnhancers = new Queue<EffectChanceEnhancer>();
            Queue<ExtraEffect> effects = new Queue<ExtraEffect>();
            

            foreach (var data in sets)
            {
                var Skill = data.Skill;

                Debug.Log(Skill.UID);

                switch (Skill.SkillType)
                {
                    case ESkillType.Active:
                        ActiveSkillSO so = Skill as ActiveSkillSO;
                        ActiveSkill skill = CreateActvieSkill(so, owner);

                        skillSet.ActiveSkills.Add(skill);

                        gets.Add(so.UID, skill);

                        break;
                    case ESkillType.Passive:
                        
                        PassiveSkillSO passiveSO = Skill as PassiveSkillSO;
                        PassiveSkill passive = CreatePassiveSkill(passiveSO, owner);
                        passive.SetUID(passiveSO.UID);

                        skillSet.PassiveSkills.Add(passive);

                        gets.Add(passiveSO.UID, passive);

                        break;

                    case ESkillType.SkillStatEnhancer:

                        EffectStatAdderData skillEnhancerData = Skill as EffectStatAdderData;
                        EffectStatEnhancer statEnhancer = new EffectStatEnhancer(skillEnhancerData);
                        statEnhancers.Enqueue(statEnhancer);

                        break;

                    case ESkillType.SkillChanceEnhancer:

                        EffectChanceAdderData skillChanceData = Skill as EffectChanceAdderData;
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
                if(gets.TryGetValue(statAdder.Data.UID, out ISkill skill))
                {
                    statAdder.ApplySkill(skill);
                    Debug.Log("Stat Add");
                }
            }
            foreach (var chacneAdder in chanceEnhancers)
            {
                if (gets.TryGetValue(chacneAdder.Data.UID, out ISkill skill))
                {
                    chacneAdder.ApplySkill(skill);
                    Debug.Log("Chance Add");
                }
            }
            foreach (var extra in effects)
            {
                if (gets.TryGetValue(extra.EffectEntry.TargetSkill.UID, out ISkill skill))
                {
                    extra.ApplySkill(skill);
                    Debug.Log("Extra");
                }
            }

            return skillSet;
        }

        private ActiveSkill CreateActvieSkill(ActiveSkillSO skillSO, Hero owner)
        {
            ITrigger trigger = GetTrigger(skillSO.Trigger);
            IFinder target = GetTarget(skillSO.Target, owner);
            IExecute execution = GetExecution(skillSO.Execution, skillSO.AnimationData, owner.SpriteChanger, owner);
            
            ActiveSkill activeSkill = new ActiveSkill(skillSO.UID, owner, trigger, target, execution);

            return activeSkill;
        }
        public ITrigger GetTrigger(TriggerSystem system)
        {
            int value = system.RequireValue;
            switch (system.Trigger)
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
        private IFinder GetTarget(TargetSystem system, Hero owner)
        {
            switch (system.TargetType)
            {
                case ESkillTargetType.Self:
                    return new SelfTargetFinder(owner);
                case ESkillTargetType.NearHeros:
                    return new NearHeroFinder(_fieldHeroService, owner, system.Radius);
                case ESkillTargetType.AllHeros:
                    return new AllHeroFinder(_fieldHeroService);
                case ESkillTargetType.NearEnemies:
                    return new NearEnemyFinder(owner, system.Radius);
                case ESkillTargetType.AllEnemies:
                    return new AllEnemyFinder(_fieldEnemyService);
            }

            return null;
        }
        private IExecute GetExecution(ExecutionSystem executionSystem, SkillAnimationData skillAnimationData, ISpriteChanger spriteChanger, Hero owner)
        {
            string name = string.Empty;

            string skillName = executionSystem.name;
            if (skillName.Contains("TargetMelee"))
            {
                name = "Skill.Data.TargetMeleeExecution";
            }
            else if (skillName.Contains("ConeMelee"))
            {
                name = "Skill.Data.ConeMeleeExecution";
            }

            Debug.Log(name);

            Type type = Type.GetType(name);

            Debug.Log(type);

            if (type != null)
            {
                object[] args = new object[] { executionSystem, skillAnimationData, spriteChanger,  _vfxService, _combatService, owner };

               return (IExecute)Activator.CreateInstance(type, args);
                
            }
            else
            {
                return null;
            }
        }
        private PassiveSkill CreatePassiveSkill(PassiveSkillSO passiveSkillSO, Hero owner)
        {
            switch (passiveSkillSO.Target.TargetType)
            {
                case ESkillTargetType.Self:
                    return new SelfPassive(owner, passiveSkillSO.Effects);
                case ESkillTargetType.NearHeros:
                    break;
                case ESkillTargetType.AllHeros:
                    break;
                case ESkillTargetType.NearEnemies:
                    break;
                case ESkillTargetType.AllEnemies:
                    break;
                default:
                    break;
            }
            return null;
        }
        public interface ISkillStatEnhancer
        {

        }
        public interface ISkillExtraEffectInjecter
        {

        }
    }
}
     