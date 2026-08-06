using Combat;
using System;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Projectiles.Data;

namespace WhatMerge.Projectiles
{
    public class LinearMove : IProjectile
    {
        private Transform _owner;
        private Vector3 _dir;
        private float _speed;
        private bool _isPiercing = false;

        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;

        public LinearMove(StraightProjectileData data, Transform owner, ICombatant target)
        {
            _dir = (target.Position - owner.position).normalized;
            _speed = data.Speed;
            _owner = owner;
            _isPiercing = data.IsPiercing;
        }

        public void Tick(float tick)
        {
            _owner.position += _dir * tick * _speed;
        }

        public void HitEnemy(IDamageable enemy)
        {
            OnExecute?.Invoke(new ProjectileImpact(enemy, enemy.Position));

            if (!_isPiercing)
                OnExpired?.Invoke();
        }
    }
    public class HomingMove : IProjectile
    {
        private const float ArrivalSqrDistance = 0.001f;

        private Transform _owner;
        private ICombatant _target;
        private float _speed;
        private bool _isCompleted;

        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;

        public HomingMove(Transform owner, ICombatant target, float speed)
        {
            _owner = owner;
            _target = target;
            _speed = speed;
        }
        public void Tick(float tick)
        {
            if (_isCompleted)
                return;

            if(_target == null || !_target.IsActive)
            {
                Expire();
                return;
            }

            Vector3 offset = _target.Position - _owner.position;
            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance <= ArrivalSqrDistance)
            {
                _owner.position = _target.Position;
                Complete(_target as IDamageable, _target.Position);
                return;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            float moveDistance = tick * _speed;
            Vector3 dir = offset / distance;

            if (moveDistance >= distance)
            {
                _owner.position = _target.Position;
                RotationToTarget(dir);
                Complete(_target as IDamageable, _target.Position);
                return;
            }

            _owner.position += dir * moveDistance;

            RotationToTarget(dir);
        }

        private void Complete(IDamageable target, Vector3 position)
        {
            if (_isCompleted)
                return;

            _isCompleted = true;
            OnExecute?.Invoke(new ProjectileImpact(target, position));
            OnExpired?.Invoke();
        }

        private void Expire()
        {
            if (_isCompleted)
                return;

            _isCompleted = true;
            OnExpired?.Invoke();
        }

        private void RotationToTarget(Vector3 dir)
        {
            float angleRad = Mathf.Atan2(dir.y, dir.x);
            float angleDeg = angleRad * Mathf.Rad2Deg - 90f;

            Quaternion targetRotation = Quaternion.Euler(0, 0, angleDeg);

            _owner.rotation = targetRotation;
        }

        public void HitEnemy(IDamageable enemy)
        {
            if (_isCompleted || !ReferenceEquals(enemy, _target))
                return;

            Complete(enemy, enemy.Position);
        }
    }
    public class Parabola : IProjectile
    {
        private Transform _owner;
        private Vector3 _startPosition;
        private Vector3 _destination;
        private float _progress;
        private float _speed;

        private bool IsArrived = false;

        private float _delayTime = 0;

        public event Action OnArrived;
        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;

        public Parabola(ParabolaProjectileData data, Transform owner, ICombatant target)
        {
            _owner = owner;
            _startPosition = owner.position;
            _destination = target.Position;
            _speed = data.Speed;
            IsArrived = false;
            _progress = 0;
            _delayTime = data.EffectDelayTime;
        }

        public void Tick(float tick)
        {
            _progress += tick * _speed;
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
                IsArrived = true;
            }

            if (IsArrived)
            {
                _delayTime -= tick;

                if(_delayTime <= 0)
                {
                    OnExecute?.Invoke(new ProjectileImpact(null, _destination));
                    OnExpired?.Invoke();
                }
            }
        }

        public void HitEnemy(IDamageable enemy) { }
    }
}
