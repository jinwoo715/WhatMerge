using Combat;
using Enemies;
using Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data 
{
    public class ActiveSkill : IActiveSkill
    {
        private SkillAnimationData _animaData;
        public ITrigger Trigger { get; private set; }
        public IFinder Search { get; private set; }
        public IExecution Excution { get; private set; }

        public ActiveSkill(SkillAnimationData animaData, ITrigger trigger, IFinder search, IExecution excution)
        {
            Trigger = trigger;
            Search = search;
            Excution = excution;
            _animaData = animaData;
        }
    }

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

    public class SkillController
    {
        private List<IActiveSkill> _activeSkills;
        private List<IPassiveSkill> _passiveSkills;

        private float _time;
        private float _mana;
        private float _hitCount;

        private float _manaChargeMultiple = 1;

        public void Update()
        {
            _time += Time.deltaTime * _manaChargeMultiple;

            ChargeMana();
        }

        private void ChargeMana()
        {
            _mana += Time.deltaTime;
        }
        private void CountUpHitCount()
        {
            _hitCount++;
        }
    }

    public struct SkillTriggerContext
    {
        public int HitCount;
        public float Mana;
    }
    public interface IActiveSkill
    {
        public ITrigger Trigger { get; }
        public IFinder Search { get; }
        public IExecution Excution { get; }
    }
    public interface IFinder
    {
        bool HasTargetInRange(Vector3 pivot);
        List<T> GetTargets<T>(Vector3 pivot) where T : class;
    }

    public interface IExecution
    {
        void Execution();
    }

    public class SkillFactory
    {
        public void CreateSkill(int level, HeroUpgradeSkillSet set)
        {
            var sets = set.GetSets(level);

            List<ActiveSkill> activeSkills = new List<ActiveSkill>();
            List<PassiveSkill> passiveSkills = new List<PassiveSkill>();

            Dictionary<int, SkillBase> gets = new Dictionary<int, SkillBase>();
            Queue<SkillEnhancer> enhancers = new Queue<SkillEnhancer>();

            foreach (var data in sets)
            {
                var Skill = data.Skill;

                switch (Skill.SkillType)
                {
                    case ESkillType.Active:
                        gets.Add(Skill.UID, Skill);
                        break;
                    case ESkillType.Passive:
                        gets.Add(Skill.UID, Skill);
                        break;
                    case ESkillType.Enhancer:
                        enhancers.Enqueue(Skill as SkillEnhancer);
                        break;
                }
            }
        }

        private ActiveSkill CreateActvieSkill(ActiveSkillSO skillSO)
        {
            ITrigger trigger = GetTrigger(skillSO.Trigger);
            IFinder target = GetTarget(skillSO.Target);
            IExecution execution = GetExecution(skillSO.Execution);
            
            ActiveSkill activeSkill = new ActiveSkill(skillSO.AnimationData, trigger, target, execution);

            return activeSkill;
        }

        public ITrigger GetTrigger(TriggerSystem system)
        {
            float value = system.RequireValue;
            switch (system.Trigger)
            {
                case ESkillTriggerType.None:
                    return new NoneRequire(value);
                case ESkillTriggerType.HitCount:
                    return new HitCountRequire(value);
                case ESkillTriggerType.Mana:
                    return new ManaRequire(value);
            }
            return default;
        }
        private IFinder GetTarget(TargetSystem system)
        {
            return new TargetFinder(system);
        }

        private IExecution GetExecution(ExecutionSystem executionSystem)
        {
            string name = executionSystem.name.Replace("Attack", "Execution");
            
            Type type = Type.GetType(name);

            if (type != null)
            {
                object[] args = new object[] { executionSystem };

                return (IExecution)Activator.CreateInstance(type, args);
            }
            else
            {
                return null;
            }
        }
    }

    public class TargetMeleeExecution : IExecution
    {
        public void Execution()
        {
            throw new NotImplementedException();
        }
    }
    public class ConeMeleeExecution : IExecution
    {
        public void Execution()
        {
            throw new NotImplementedException();
        }
    }
    public class TargetProjectile : IExecution
    {
        public void Execution()
        {
            throw new NotImplementedException();
        }
    }

    public class SelfTargetFinder : IFinder
    {
        private Hero _owner;

        public SelfTargetFinder(Hero owner)
        {
            _owner = owner;
        }

        public List<T> GetTargets<T>(Vector3 pivot) where T : class
        {
            if (_owner is T target)
                return new List<T> { target };

            return new List<T>();
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            if (_owner.IsActive)
                return true;
   
            return false;
        }
    }
    public class NearHeroFinder : IFinder
    {
        private int _range;
        private Hero _pivot;
        private IFieldHeroService _fieldHero;
        public NearHeroFinder(IFieldHeroService fieldHero, int range)
        {
            _fieldHero = fieldHero;
        }
        public List<T> GetTargets<T>(Vector3 pivot) where T : class
        {
            var heros = _fieldHero.GetNearHeros(_pivot.OccupiedTile, _range);

            List<T> results = new List<T>();

            foreach (var hero in heros)
            {
                results.Add(hero as T);
            }

            return results;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            if (_pivot.IsActive)
                return true;

            return false;
        }
    }

    public class TargetFinder : IFinder
    {
        private float _radius;
        private IFieldHeroService _fieldHero;
        private IFieldEnemyService _enemyService;
        private ESkillTargetType _type;

        public TargetFinder(TargetSystem targetSystem)
        {
            _type = targetSystem.TargetType;
            _radius = targetSystem.Radius;
        }

        public List<T> GetTargets<T>(Vector3 pivot) where T : class
        {
            throw new NotImplementedException();
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            switch (_type)
            {
                case ESkillTargetType.Self:
                case ESkillTargetType.NearHeros:
                case ESkillTargetType.AllHeros:
                    return true;
                case ESkillTargetType.NearEnemies:
                    return SearchUtility.IsExistEnemyInRange(pivot, _radius);
                case ESkillTargetType.AllEnemies:
                    return _enemyService.GetActiveEnemyCount > 0;
            }
            return false;
        }
    }
    public interface ITrigger
    {
        bool IsMeetTrigger(SkillTriggerContext context);
    }
    public abstract class TriggerBase : ITrigger
    {
        public readonly float RequiredValue;
        public TriggerBase(float requiredValue)
        {
            RequiredValue = requiredValue;
        }
        public abstract bool IsMeetTrigger(SkillTriggerContext context);
    }
    public class NoneRequire : TriggerBase
    {
        public NoneRequire(float requiredValue) : base(requiredValue) { }

        public override bool IsMeetTrigger(SkillTriggerContext context)
        {
            return true;
        }
    }
    public class ManaRequire : TriggerBase
    {
        private float _current;
        public ManaRequire(float requiredValue) : base(requiredValue) { }
        public override bool IsMeetTrigger(SkillTriggerContext context)
        {
            _current += context.Mana;

            return _current >= RequiredValue;
        }
    }
    public class HitCountRequire : TriggerBase
    {
        private int _current;
        public HitCountRequire(float requiredValue) : base(requiredValue) { }
        public override bool IsMeetTrigger(SkillTriggerContext context)
        {
            _current += context.HitCount;
            return _current >= RequiredValue;
        }
    }
}
     