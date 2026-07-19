using System;
using System.Collections.Generic;
using System.Linq;
using WhatMerge.Heros;

namespace Skill.Data
{
    //TODO 패시브 스킬
    public abstract class PassiveSkill : IPassiveSkill
    {
        public int SkillUID { get; private set; }
        public void SetUID(int uid) { SkillUID = uid; }
        public abstract void Apply();
        public abstract void Release();
    }

    public abstract class BuffPassiveSkill : PassiveSkill
    {
        public void ApplyBuff(IHeroStatModifier statModifier, List<BuffData> effects)
        {
            foreach (var effect in effects)
            {
                if (effect is BuffData buff)
                {
                    statModifier.AddMultiplier(buff.BuffType, buff.IncreaseRatio);
                }
            }
        }
        public void RevertBuff(IHeroStatModifier statModifier, List<BuffData> effects)
        {
            foreach (var effect in effects)
            {
                if (effect is BuffData buff)
                {
                    statModifier.AddMultiplier(buff.BuffType, -buff.IncreaseRatio);
                }
            }
        }
    }

    public class SelfBuffPassive : BuffPassiveSkill
    {
        public IHeroStatModifier _statModifier;
        public List<BuffData> _effects;
        public SelfBuffPassive(IHeroStatModifier statModifier, List<BuffData> effects)
        {
            _statModifier = statModifier;
            _effects = effects;
        }
        public override void Apply()
        {
            ApplyBuff(_statModifier, _effects);
        }
        public override void Release() 
        {
            RevertBuff(_statModifier, _effects);
        }
    }
    public class NearHeroBuffPassive : BuffPassiveSkill
    {
        private IFieldHeroService _fieldHeroService;
        private Hero _owner;
        private List<BuffData> _effects;
        HashSet<Hero> _appliedHeros = new HashSet<Hero>();

        public NearHeroBuffPassive(IFieldHeroService fieldHeroService, Hero owner, List<BuffData> effects)
        {
            _fieldHeroService = fieldHeroService;
            _owner = owner;
            _effects = effects;

            _fieldHeroService.OnChangedHeroPosition += Apply;
        }

        public override void Apply()
        {
            HashSet<Hero> nearHeros = _fieldHeroService.GetNearHeros(_owner.OccupiedTile, 1).ToHashSet();

            var entered = nearHeros.Except(_appliedHeros);
            var exited = _appliedHeros.Except(nearHeros);

            foreach (var enter in entered)
            {
                ApplyBuff(enter.StatModify, _effects);
            }
            foreach (var exit in exited)
            {
                RevertBuff(exit.StatModify, _effects);
            }

            _appliedHeros = nearHeros;
        }

        public override void Release()
        {
            foreach (var hero in _appliedHeros)
            {
                RevertBuff(hero.StatModify, _effects);
            }
        }
    }
    public class AllHeroBuffPassive : BuffPassiveSkill
    {
        private IFieldHeroService _fieldHeroService;
        private Hero _owner;
        private List<BuffData> _effects;

        private Action<Hero> OnSpawnHeroBuffApply;
        private Action<Hero> OnDespawnHeroBuffRelease;

        public AllHeroBuffPassive(IFieldHeroService fieldHeroService, Hero owner, List<BuffData> effects)
        {
            _fieldHeroService = fieldHeroService;
            _owner = owner;
            _effects = effects;

            OnSpawnHeroBuffApply += (hero) => ApplyBuff(hero.StatModify, _effects);
            OnDespawnHeroBuffRelease += (hero) => RevertBuff(hero.StatModify, _effects);

            _fieldHeroService.OnSpawnedHero += OnSpawnHeroBuffApply;
            _fieldHeroService.OnDestroyHero += OnDespawnHeroBuffRelease;
        }

        public override void Apply()
        {
            var allHeros = _fieldHeroService.GetAllFieldHero;

            foreach (var hero in allHeros)
            {
                ApplyBuff(hero.StatModify, _effects);
            }
        }

        public override void Release()
        {
            _fieldHeroService.OnSpawnedHero -= OnSpawnHeroBuffApply;
            _fieldHeroService.OnDestroyHero -= OnDespawnHeroBuffRelease;

            var allHeros = _fieldHeroService.GetAllFieldHero;

            foreach (var hero in allHeros)
            {
                RevertBuff(hero.StatModify, _effects);
            }
        }
    }
}
