using Combat;
using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Projectile
{
    public class LinearMove : IProjectileMoveStrategy
    {
        private Transform _owner;
        private Vector3 _dir;
        private float _speed;

        public bool IsArrived { get; set; }

        public event Action OnArrived;

        public LinearMove(Transform owner, ICombatant target, float speed)
        {
            _dir = (target.Position - owner.position).normalized;
            _speed = speed;
            _owner = owner;
        }

        public void Tick(float tick)
        {
            _owner.position += _dir * tick * _speed;
        }
    }
    public class HomingMove : IProjectileMoveStrategy
    {
        private Transform _owner;
        ICombatant _target;
        private float _speed;

        public bool IsArrived { get; private set; }

        public event Action OnArrived;

        public HomingMove(Transform owner, ICombatant target, float speed)
        {
            _owner = owner;
            _target = target;
            _speed = speed;
        }
        public void Tick(float tick)
        {
            Vector3 dir = (_target.Position - _owner.position).normalized;
            _owner.position += dir * Time.deltaTime * _speed;

            RotationToTarget(dir);

            float distance = Vector3.SqrMagnitude(_owner.position - _target.Position);
            if (distance <= 0.001f)
            {
                _owner.transform.position = _target.Position;
                OnArrived?.Invoke();
            }
        }

        private void RotationToTarget(Vector3 dir)
        {
            float angleRad = Mathf.Atan2(dir.y, dir.x);
            float angleDeg = angleRad * Mathf.Rad2Deg - 90f;

            Quaternion targetRotation = Quaternion.Euler(0, 0, angleDeg);

            _owner.rotation = targetRotation;
        }
    }
    public class Parabola : IProjectileMoveStrategy
    {
        private Transform _owner;
        private Vector3 _startPosition;
        private Vector3 _destination;
        private float _progress;
        private float _speed;

        private bool IsArrived;

        public event Action OnArrived;

        public void Init(Transform owner, ICombatant target, float speed)
        {
            _owner = owner;
            _startPosition = owner.position;
            _destination = target.Position;
            _speed = speed;
            _progress = 0;
        }

        public void Tick(float tick)
        {
            if (IsArrived) return;

            _progress += Time.deltaTime * _speed;
            float t = Mathf.Clamp01(_progress);

            // 1. 기본 직선 보간
            Vector3 pos = Vector3.Lerp(_startPosition, _destination, t);

            // 2. 포물선(y) 추가 (sin 기반 아크)
            float height = Mathf.Sin(t * Mathf.PI) * 0.9f;
            pos.y += height;

            _owner.position = pos;

            float distance = Vector3.SqrMagnitude(_owner.position - _destination);
            if (distance <= 0.001f)
            {
                OnArrived?.Invoke();
            }
        }
    }
}
