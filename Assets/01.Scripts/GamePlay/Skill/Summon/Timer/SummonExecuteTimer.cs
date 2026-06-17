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
        private float _nextApplyTime;
        private bool _isApplied;

        public event Action OnExecute;
        public event Action OnTimeOut;

        public void Init(float lifeTime, SummonApplyTiming applyTiming)
        {
            _lifeTime = lifeTime;
            _currentTime = 0;
            _applyTiming = applyTiming;
            _nextApplyTime = _applyTiming.Delay;
            _isApplied = false;
        }

        public void Tick()
        {
            _currentTime += Time.deltaTime;

            if (_isApplied && _applyTiming.ApplyType == SummonApplyType.Once)
            {
                CheckTimeout();
                return;
            }

            if (_currentTime >= _nextApplyTime)
            {
                _isApplied = true;

                _nextApplyTime += _applyTiming.Delay;

                OnExecute?.Invoke();
            }

            CheckTimeout();
        }

        private void CheckTimeout()
        {
            if (_currentTime >= _lifeTime)
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}