using Combat;
using Skill.Data;
using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Summon
{
    public class NoneMoveStrategy : ISummonMoveStrategy
    {
        public event Action OnTargetLost;

        public void Tick(float tick) { }
    }
    public class AttachMoveStrategy : ISummonMoveStrategy
    {
        public event Action OnTargetLost;

        private ICombatant _target;
        private Transform _owner;

        public TargetLostEventType _targetLostEventType;

        public AttachMoveStrategy(Transform owner, ICombatant target)
        {
            _target = target;
            _owner = owner;
        }

        public void Tick(float tick)
        {
            if (_target.IsActive)
                _owner.transform.position = _target.Position;
            else
                OnTargetLost?.Invoke();
        }
    }
    public class ToTargetMoveStrategy : ISummonMoveStrategy
    {
        private ICombatant _target;
        private Transform _owner;

        private float _duration;
        private float _current;

        private Vector3 _enemyDeltaPosition;
        private Vector3 _origin;

        public event Action OnTargetLost;

        public ToTargetMoveStrategy(Transform owner, ICombatant target, SummonItemData data)
        {
            _target = target;
            _owner = owner;
            _duration = data.Duration;
            _current = 0;

            //var spawnType = (data as MoveableSummonData).SpawnPoint;
            //owner.position += GetSpawnPosition(spawnType);

            _origin = owner.position;
            _enemyDeltaPosition = _target.Position;
        }

        private Vector3 GetSpawnPosition(SpawnPointType spawnPointType)
        {
            Vector3 position = Vector3.zero;
            switch (spawnPointType)
            {
                case SpawnPointType.Up:
                    position += Vector3.up;
                    break;
                case SpawnPointType.Right:
                    position += Vector3.right;
                    break;
                case SpawnPointType.Down:
                    position += Vector3.down;
                    break;
                case SpawnPointType.Left:
                    position += Vector3.left;
                    break;
            }

            return position;
        }

        public void Tick(float tick)
        {
            _current += tick;

            if (!_target.IsActive)
            {
                OnTargetLost?.Invoke();
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
