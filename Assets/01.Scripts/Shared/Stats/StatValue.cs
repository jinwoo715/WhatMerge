namespace WhatMerge.Stats
{
    public class StatValue
    {
        private float _baseValue;
        private float _fixedValue;
        private float _multiplier = 1f;

        public float Value => (_baseValue + _fixedValue) * _multiplier;

        public void SetBaseValue(float value)
        {
            _baseValue = value;
            _fixedValue = 0f;
            _multiplier = 1f;
        }

        public void AddFixedValue(float value)
        {
            _fixedValue += value;
        }

        public void AddMultiplier(float value)
        {
            _multiplier += value;
        }
    }
}