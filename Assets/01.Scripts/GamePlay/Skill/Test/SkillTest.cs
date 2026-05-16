using System.Collections;

namespace Skill.Data 
{
    public class ActiveSkill : IActiveSkill
    {
        private ActiveSkillData _data;
        public ITrigger Trigger { get; private set; }
        public ITarget Search { get; private set; }
        public IExcution Excution { get; private set; }

        public ActiveSkill(ActiveSkillData data, ITrigger trigger, ITarget search, IExcution excution)
        {
            Trigger = trigger;
            Search = search;
            Excution = excution;
            _data = data;
        }
    }
    public class ActiveSkillData
    {
        //Info
        public int UID;
        public string Name;
        public string Description;
        
        //Strategy
        public string TriggerUID;
        public string SearchUID;
        public string ExcutionUID;

        //Sprite Motion
        public float ReadyMotionTime;
        public float ExcutionMotionTime;

        //Apply Excution Value From Damage
        public int ApplyValueRatio;
    }
    public struct SkillTriggerContext
    {
        public int HitCount;
        public float Mana;
    }
    public interface IActiveSkill
    {
        public ITrigger Trigger { get; }
        public ITarget Search { get; }
        public IExcution Excution { get; }
    }
    public interface ITarget
    {
        bool HasTargetInRange();
    }
    public interface IExcution
    {
        IEnumerator Excution();
    }

    public class SkillFactory
    {
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
     