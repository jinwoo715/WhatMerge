using Combat;
using System;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Projectiles.Data;

namespace WhatMerge.Projectiles
{
    public class LinearMove : IProjectile
    {
        private readonly Transform _owner;
        private readonly Vector3 _dir;
        private readonly float _speed;
        private readonly bool _isPiercing;
        private bool _isCompleted;

        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;

        public LinearMove(StraightProjectileData data, Transform owner, ICombatant target)
        {
            _dir = (target.Position - owner.position).normalized;
            _speed = data.Speed;
            _owner = owner;
            _isPiercing = data.IsPiercing;

            ProjectileRotation.Apply(_owner, _dir, data.RotationOffset);
        }

        public void Tick(float tick)
        {
            if (_isCompleted)
                return;

            _owner.position += _dir * tick * _speed;
        }

        public void HitEnemy(IDamageable enemy)
        {
            if (_isCompleted || enemy == null || !enemy.IsActive)
                return;

            if (!_isPiercing)
                _isCompleted = true;

            OnExecute?.Invoke(new ProjectileImpact(enemy, enemy.Position));

            if (!_isPiercing)
                OnExpired?.Invoke();
        }
    }

    public class HomingMove : IProjectile
    {
        private const float ArrivalSqrDistance = 0.001f;

        private readonly Transform _owner;
        private readonly ICombatant _target;
        private readonly float _speed;
        private readonly float _rotationOffset;
        private bool _isCompleted;

        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;

        public HomingMove(HomingProjectileData data, Transform owner, ICombatant target)
        {
            _owner = owner;
            _target = target;
            _speed = data.Speed;
            _rotationOffset = data.RotationOffset;

            ProjectileRotation.Apply(_owner, target.Position - owner.position, _rotationOffset);
        }

        public void Tick(float tick)
        {
            if (_isCompleted)
                return;

            if (_target == null || !_target.IsActive)
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
                ProjectileRotation.Apply(_owner, dir, _rotationOffset);
                Complete(_target as IDamageable, _target.Position);
                return;
            }

            _owner.position += dir * moveDistance;
            ProjectileRotation.Apply(_owner, dir, _rotationOffset);
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

        public void HitEnemy(IDamageable enemy)
        {
            if (_isCompleted || !ReferenceEquals(enemy, _target))
                return;

            Complete(enemy, enemy.Position);
        }
    }

    public class Parabola : IProjectile
    {
        private const float ArcHeight = 0.9f;

        private readonly Transform _owner;
        private readonly Vector3 _startPosition;
        private readonly Vector3 _destination;
        private readonly float _speed;
        private readonly float _rotationOffset;
        private float _progress;
        private bool _isArrived;
        private bool _isCompleted;
        private float _delayTime;

        public event Action OnArrived;
        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;

        public Parabola(ParabolaProjectileData data, Transform owner, ICombatant target)
        {
            _owner = owner;
            _startPosition = owner.position;
            _destination = target.Position;
            _speed = data.Speed;
            _rotationOffset = data.RotationOffset;
            _progress = 0f;
            _delayTime = data.EffectDelayTime;

            Vector3 initialDirection = _destination - _startPosition;
            initialDirection.y += Mathf.PI * ArcHeight;
            ProjectileRotation.Apply(_owner, initialDirection, _rotationOffset);
        }

        public void Tick(float tick)
        {
            if (_isCompleted)
                return;

            _progress += tick * _speed;
            float t = Mathf.Clamp01(_progress);
            Vector3 previousPosition = _owner.position;
            Vector3 position = Vector3.Lerp(_startPosition, _destination, t);
            position.y += Mathf.Sin(t * Mathf.PI) * ArcHeight;

            _owner.position = position;
            ProjectileRotation.Apply(_owner, position - previousPosition, _rotationOffset);

            if (!_isArrived && t >= 1f)
            {
                _isArrived = true;
                OnArrived?.Invoke();
            }

            if (!_isArrived)
                return;

            _delayTime -= tick;
            if (_delayTime > 0f)
                return;

            _isCompleted = true;
            OnExecute?.Invoke(new ProjectileImpact(null, _destination));
            OnExpired?.Invoke();
        }

        public void HitEnemy(IDamageable enemy) { }
    }

    internal static class ProjectileRotation
    {
        private const float DirectionSqrEpsilon = 0.000001f;

        public static void Apply(Transform projectile, Vector3 direction, float rotationOffset)
        {
            Vector2 direction2D = direction;
            if (direction2D.sqrMagnitude <= DirectionSqrEpsilon)
                return;

            float angle = Mathf.Atan2(direction2D.y, direction2D.x) * Mathf.Rad2Deg;
            projectile.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }
    }
}
