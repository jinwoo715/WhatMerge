using Combat;
using System;
using UnityEngine;

namespace Skill.Summon
{
    public class NoneMoveStrategy : ISummonMoveStrategy
    {
        public event Action OnLooseTarget;
        public void Init(Transform owner, ICreature target, float speed) { }
        public void Tick() { }
    }
    public class AttachMoveStrategy : ISummonMoveStrategy
    {
        public event Action OnLooseTarget;

        private ICreature _target;
        private Transform _owner;

        public void Init(Transform owner, ICreature target, float speed)
        {
            _target = target;
            _owner = owner;
        }

        public void Tick()
        {
            if(_target.IsActive)
                _owner.transform.position = _target.Position;
        }
    }
    public class ToTargetMoveStrategy : ISummonMoveStrategy
    {
        private ICreature _target;
        private Transform _owner;

        private float _speed;

        private Vector3 _dir;
        private Vector3 _enemyDeltaPosition;

        private bool isArrived = false;

        public event Action OnLooseTarget;

        public void Init(Transform owner, ICreature target, float speed)
        {
            _target = target;
            _owner = owner;
            _speed = speed;
            _enemyDeltaPosition = _target.Position;
            _dir = (_target.Position - _owner.position).normalized;
            isArrived = false;
        }

        public void Tick()
        {
            if (!_target.IsActive)
            {
                OnLooseTarget?.Invoke();
                return;
            }

            if (isArrived)
            {
                _owner.position = _target.Position;
            }
            else
            {
                Vector3 moveAmount = _target.Position - _enemyDeltaPosition;
                _enemyDeltaPosition = _target.Position;

                _owner.position += moveAmount;
                _owner.position += _dir * Time.deltaTime * _speed;

                if (Vector3.SqrMagnitude(_owner.position - _target.Position) < 0.001f)
                    isArrived = true;
            }
        }
    }
}