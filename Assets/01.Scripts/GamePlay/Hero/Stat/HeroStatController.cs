using Heros.Stat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stat
{
    public enum EHeroStat
    {
        Damage,
        AttackSpeed,
        EnguageSpeed,
        AttackRange,
        FixPenetration,
        RatioPenetration,
    }

    public class HeroStatController : IHeroStatModifier, IHeroStatReadOnly
    {
        private HeroStat _damage = new HeroStat();
        private HeroStat _attackSpeed = new HeroStat();
        private HeroStat _enguageSpeed = new HeroStat();
        private HeroStat _attackRange = new HeroStat();

        private Dictionary<EHeroStat, HeroStat> _stats = new Dictionary<EHeroStat, HeroStat>();

        public event Action<EHeroStat, float> OnStatChange;

        public void SetBaseValue(EHeroStat stat, float value)
        {
            switch (stat)
            {
                case EHeroStat.Damage:
                    _damage.Init(value);
                    break;
                case EHeroStat.AttackSpeed:
                    _attackSpeed.Init(value);
                    break;
                case EHeroStat.EnguageSpeed:
                    break;
                case EHeroStat.AttackRange:
                    break;
            }

            OnStatChange?.Invoke(stat, GetStat(stat));
        }

        public float GetStat(EHeroStat stat)
        {
            switch (stat)
            {
                case EHeroStat.Damage:
                    return _damage.FinalValue;
                case EHeroStat.AttackSpeed:
                    
                    return _attackSpeed.FinalValue;
                case EHeroStat.EnguageSpeed:
                    return _enguageSpeed.FinalValue;
                case EHeroStat.AttackRange:
                    return _attackRange.FinalValue;
                default:
                    return 0;
            }
        }

        public void AddFixedStatValue(EHeroStat stat, float value)
        {
            OnStatChange?.Invoke(stat, GetStat(stat));
        }

        public void AddMultiplyValue(EHeroStat stat, float value)
        {
            OnStatChange?.Invoke(stat, GetStat(stat));
        }
    }
}