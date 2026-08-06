using System;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Summons.Data;

namespace WhatMerge.Summons
{
    public class NoneMoveStrategy : ISummonMoveStrategy
    {
        public event Action<TargetLostEventType> OnTargetLost;

        public void Tick(float tick) { }
    }
    public class AttachMoveStrategy : ISummonMoveStrategy
    {
        public event Action<TargetLostEventType> OnTargetLost;

        private ICombatant _target;
        private Transform _owner;

        public TargetLostEventType _targetLostEventType;

        public AttachMoveStrategy(Transform owner, ICombatant target, TargetLostEventType targetLostEventType)
        {
            _target = target;
            _owner = owner;
            _targetLostEventType = targetLostEventType;
        }

        public void Tick(float tick)
        {
            if (_target.IsActive)
                _owner.transform.position = _target.Position;
            else
                OnTargetLost?.Invoke(_targetLostEventType);
        }
    }
    public class ApproachMoveStrategy : ISummonMoveStrategy
    {
        private ICombatant _target;
        private Transform _owner;

        private float _duration;
        private float _current;

        private Vector3 _enemyDeltaPosition;
        private Vector3 _origin;

        public TargetLostEventType _targetLostEventType;

        public event Action<TargetLostEventType> OnTargetLost;

        public ApproachMoveStrategy(Transform owner, ICombatant target, float duration, TargetLostEventType targetLostEventType)
        {
            _target = target;
            _owner = owner;
            _duration = duration;
            _current = 0;

            _targetLostEventType = targetLostEventType;

            _origin = owner.position;
            _enemyDeltaPosition = _target.Position;
        }

        public void Tick(float tick)
        {
            _current += tick;

            if (!_target.IsActive)
            {
                OnTargetLost?.Invoke(_targetLostEventType);
                return;
            }

            Vector3 moveAmount = _target.Position - _enemyDeltaPosition;
            _enemyDeltaPosition = _target.Position;

            _origin += moveAmount;

            if (_duration <= 0f)
            {
                _owner.position = _target.Position;
                return;
            }

            float lerp = Mathf.Clamp01(_current / _duration);
            _owner.position = Vector3.Lerp(_origin, _target.Position, lerp);
        }
    }
}
