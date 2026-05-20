using Combat;
using Enemies;
using Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data 
{
    public class PassiveSkill : IPassiveSkill
    {
        public int UID => throw new System.NotImplementedException();

        public void Apply()
        {
            throw new System.NotImplementedException();
        }

        public void ModifyParam(int paramIndex, float value)
        {
            throw new System.NotImplementedException();
        }

        public void Remove()
        {
            throw new System.NotImplementedException();
        }
    }

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

    public class SkillFactory
    {
        private IVFXService _vfxService;
        private IAttackRegister _combatService;
        private IFieldHeroService _fieldHeroService;
        private IFieldEnemyService _fieldEnemyService;

        public void Init(IVFXService vfxService, IAttackRegister attackRegister, IFieldHeroService fieldHeroService, IFieldEnemyService fieldEnemyService)
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

            List<PassiveSkill> passiveSkills = new List<PassiveSkill>();

            Dictionary<int, SkillBase> gets = new Dictionary<int, SkillBase>();
            Queue<SkillEnhancer> enhancers = new Queue<SkillEnhancer>();

            foreach (var data in sets)
            {
                var Skill = data.Skill;

                Debug.Log(Skill.UID);

                switch (Skill.SkillType)
                {
                    case ESkillType.Active:
                        gets.Add(Skill.UID, Skill);

                        ActiveSkillSO so = Skill as ActiveSkillSO;

                        Debug.Log(Skill);
                        Debug.Log(so);

                        ActiveSkill skill = CreateActvieSkill(so, owner);

                        skillSet.ActiveSkills.Add(skill);

                        break;
                    case ESkillType.Passive:
                        gets.Add(Skill.UID, Skill);
                        break;
                    case ESkillType.Enhancer:
                        enhancers.Enqueue(Skill as SkillEnhancer);
                        break;
                }
            }

            return skillSet;
        }
        private ActiveSkill CreateActvieSkill(ActiveSkillSO skillSO, Hero owner)
        {
            ITrigger trigger = GetTrigger(skillSO.Trigger);
            IFinder target = GetTarget(skillSO.Target, owner);
            IExecute execution = GetExecution(skillSO.Execution, skillSO.AnimationData, owner.SpriteChanger, owner);
            
            ActiveSkill activeSkill = new ActiveSkill(owner, trigger, target, execution);

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
    }
}
     