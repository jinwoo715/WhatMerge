using Skill.Data;
using System;
using UnityEngine;

namespace Skill.Summon
{
    public class SummonExecuteTimer
    {
        private float _lifeTime;
        private SummonApplyTiming _applyTiming;
        private float _currentTime;
        private bool _isApplied;

        public event Action OnExecute;
        public event Action OnTimeOut;

        public void Init(float lifeTime, SummonApplyTiming applyTiming)
        {
            _lifeTime = lifeTime;
            _currentTime = 0;
            _applyTiming = applyTiming;
            _isApplied = false;
        }

        public void Tick()
        {
            _currentTime += Time.deltaTime;

            if (_isApplied && !_applyTiming.IsIntervalApply)
                return;

            if (_currentTime >= _applyTiming.Delay)
            {
                _isApplied = true;
                OnExecute?.Invoke();
            }

            if (_currentTime >= _lifeTime)
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}