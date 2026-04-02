using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Heros.Stat
{
    public class HeroStat
    {
        private float _baseValue;
        private float _addFixedValue;
        private float _multiplier;

        public float FinalValue => (_baseValue + _addFixedValue) * _multiplier;

        public void Init(float baseValue)
        {
            _baseValue = baseValue;
            _addFixedValue = 0;
            _multiplier = 1;
        }

        public void AddFixedValue(float value)
        {
            _addFixedValue += value;
        }
        public void AddMultiplier(float value)
        {
            _multiplier += value;
        }
    }
}