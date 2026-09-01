using System;
using System.Collections.Generic;
using WhatMerge.Stats;

namespace WhatMerge.Heros
{
    public class HeroStats : IHeroStatModifier, IHeroStatReadOnly
    {
        private Dictionary<HeroStatType, StatValue> _stats = new Dictionary<HeroStatType, StatValue>();
        public event Action<HeroStatType, float> OnStatChanged;

        public HeroStats()
        {
            foreach (HeroStatType type in System.Enum.GetValues(typeof(HeroStatType)))
            {
                _stats.Add(type, new StatValue());
            }
        }

        public void SetBaseValue(HeroStatType type, float value)
        {
            _stats[type].SetBaseValue(value);
            OnStatChanged?.Invoke(type, GetStat(type));
        }

        public void Reset()
        {
            foreach (StatValue stat in _stats.Values)
                stat.Reset();
        }

        public void AddFixedValue(HeroStatType type, float value)
        {
            _stats[type].AddFixedValue(value);
            OnStatChanged?.Invoke(type, GetStat(type));
        }

        public void AddMultiplier(HeroStatType type, float value)
        {
            _stats[type].AddMultiplier(value);
            OnStatChanged?.Invoke(type, GetStat(type));
        }

        public float GetStat(HeroStatType type)
        {
            return _stats[type].Value;
        }
    }
}
