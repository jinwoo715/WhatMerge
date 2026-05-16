using Entity;
using Heros;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skill
{
    public abstract class BuffBase : IPassiveSkill
    {
        protected Hero _owner;
        protected BuffData _data;
        protected IFieldHeroService _fieldHeroService;
        private float _addValue;

        public float Value => _data.Value + _addValue;
        public int UID => _data.UID;

        public virtual void Init(IFieldHeroService fieldHeroService, Hero owner, BuffData data)
        {
            _owner = owner;
            _data = data;
            _fieldHeroService = fieldHeroService;
        }
        public void ModifyParam(int paramIndex, float value)
        {
            _addValue += value;
        }

        public void ApplyBuff(Hero target)
        {
            target.ModifyStat(_data.StatType, Value);
        }
        public void RevertBuff(Hero target)
        {
            target.ModifyStat(_data.StatType, -Value);
        }

        public abstract void Apply();
        public abstract void Remove();
    }
    public class SelfBuff : BuffBase
    {
        public override void Apply()
        {
            ApplyBuff(_owner);
        }
        public override void Remove()
        {
            RevertBuff(_owner);
        }
    }
    public class NearBuff : BuffBase
    {
        HashSet<Hero> _appliedHeros = new HashSet<Hero>();
        public override void Init(IFieldHeroService fieldHeroService, Hero owner, BuffData data)
        {
            base.Init(fieldHeroService, owner, data);
            fieldHeroService.OnChangedFieldHero += ApplyNearHeroBuff;
        }
        public override void Apply()
        {
            ApplyNearHeroBuff();
        }
        public void ApplyNearHeroBuff()
        {
            HashSet<Hero> nearHeros = _fieldHeroService.GetNearHeros(_owner.OccupiedTile, 1).ToHashSet();

            var entered = nearHeros.Except(_appliedHeros);
            var exited = _appliedHeros.Except(nearHeros);

            foreach (var enter in entered)
            {
                ApplyBuff(enter);
            }
            foreach (var exit in exited)
            {
                RevertBuff(exit);
            }

            _appliedHeros = nearHeros;
        }
        public override void Remove()
        {
            _fieldHeroService.OnChangedFieldHero -= ApplyNearHeroBuff;

            foreach (var hero in _appliedHeros)
            {
                RevertBuff(hero);
            }

            _appliedHeros.Clear();
        }
    }

    public class AllBuff : BuffBase
    {
        public override void Init(IFieldHeroService fieldHeroService, Hero owner, BuffData data)
        {
            base.Init(fieldHeroService, owner, data);

            fieldHeroService.OnSpawnedHero += ApplyBuff;
            fieldHeroService.OnDestroyHero += RevertBuff;
        }
        public override void Apply()
        {
            var allHeros = _fieldHeroService.GetAllFieldHero;
            foreach (var hero in allHeros)
            {
                ApplyBuff(hero);
            }
        }
        public override void Remove()
        {
            var allHeros = _fieldHeroService.GetAllFieldHero;
            foreach (var hero in allHeros)
            {
                RevertBuff(hero);
            }

            _fieldHeroService.OnSpawnedHero -= ApplyBuff;
            _fieldHeroService.OnDestroyHero -= RevertBuff;
        }
    }
}