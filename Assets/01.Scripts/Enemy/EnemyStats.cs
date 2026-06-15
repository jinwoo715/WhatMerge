using System.Collections.Generic;
using WhatMerge.Stats;

namespace WhatMerge.Enemies
{
    public sealed class EnemyStats : IEnemyStatReadOnly, IEnemyStatModifier
    {
        private Dictionary<EnemyStatType, StatValue> _stats = new Dictionary<EnemyStatType, StatValue>();

        public EnemyStats()
        {
            foreach (EnemyStatType type in System.Enum.GetValues(typeof(EnemyStatType)))
            {
                _stats.Add(type, new StatValue());
            }
        }

        public void SetBaseValue(EnemyStatType type, float value)
        {
            _stats[type].SetBaseValue(value);
        }

        public void AddFixedValue(EnemyStatType type, float value)
        {
            _stats[type].AddFixedValue(value);
        }

        public void AddMultiplier(EnemyStatType type, float value)
        {
            _stats[type].AddMultiplier(value);
        }

        public float GetStat(EnemyStatType type)
        {
            return _stats[type].Value;
        }
    }
}
