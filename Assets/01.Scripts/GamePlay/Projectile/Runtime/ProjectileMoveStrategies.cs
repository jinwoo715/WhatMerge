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

        private readonly ProjectileRotate _rotateData;

        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;


        public LinearMove(StraightProjectileData data, Transform owner, ICombatant target)
        {
            _dir = (target.Position - owner.position).normalized;
            _speed = data.Speed;
            _owner = owner;
            _isPiercing = data.IsPiercing;

            _rotateData = data.RotateData;

            ProjectileRotation.Apply(_owner, _dir, data.RotationOffset);
        }

        public void Tick(float tick)
        {
            if (_isCompleted)
                return;

            _owner.position += _dir * tick * _speed;

            ProjectileRotation.TryApplySpin(_owner, _rotateData, tick);
        }

        public void HitTarget(ICombatant target)
        {
            if (_isCompleted || target == null || !target.IsActive)
                return;

            if (!_isPiercing)
                _isCompleted = true;

            OnExecute?.Invoke(new ProjectileImpact(target, target.Position));

            if (!_isPiercing)
                OnExpired?.Invoke();
        }

        public void Dispose()
        {
            _isCompleted = true;
            OnExecute = null;
            OnExpired = null;
        }
    }

    public class HomingMove : IProjectile
    {
        private const float ArrivalSqrDistance = 0.001f;

        private readonly Transform _owner;
        private ICombatant _target;
        private readonly float _speed;
        private readonly float _rotationOffset;
        private bool _isCompleted;
        private bool _disposed;

        private readonly ProjectileRotate _rotateData;

        public event Action<ProjectileImpact> OnExecute;
        public event Action OnExpired;

        public HomingMove(HomingProjectileData data, Transform owner, ICombatant target)
        {
            _owner = owner;
            _target = target;
            _speed = data.Speed;
            _rotationOffset = data.RotationOffset;

            _rotateData = data.RotateData;

            _target.OnActiveOff += OnTargetInactive;

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
                Complete(_target, _target.Position);
                return;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            float moveDistance = tick * _speed;
            Vector3 dir = offset / distance;

            if (moveDistance >= distance)
            {
                _owner.position = _target.Position;
                if (!ProjectileRotation.TryApplySpin(_owner, _rotateData, tick))
                    ProjectileRotation.Apply(_owner, dir, _rotationOffset);

                Complete(_target, _target.Position);
                return;
            }

            _owner.position += dir * moveDistance;

            if (!ProjectileRotation.TryApplySpin(_owner, _rotateData, tick))
                ProjectileRotation.Apply(_owner, dir, _rotationOffset);
        }

        private void Complete(ICombatant target, Vector3 position)
        {
            if (_isCompleted)
                return;

            _isCompleted = true;
            ReleaseTarget();
            OnExecute?.Invoke(new ProjectileImpact(target, position));
            OnExpired?.Invoke();
        }

        private void Expire()
        {
            if (_isCompleted)
                return;

            _isCompleted = true;
            ReleaseTarget();
            OnExpired?.Invoke();
        }

        public void HitTarget(ICombatant target)
        {
            if (_isCompleted || !ReferenceEquals(target, _target))
                return;

            Complete(target, target.Position);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _isCompleted = true;
            ReleaseTarget();
            OnExecute = null;
            OnExpired = null;
        }

        private void OnTargetInactive(ICombatant target)
        {
            if (!ReferenceEquals(target, _target))
                return;

            ReleaseTarget();
            Expire();
        }

        private void ReleaseTarget()
        {
            ICombatant target = _target;
            _target = null;
            if (target != null)
                target.OnActiveOff -= OnTargetInactive;
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

        private readonly ProjectileRotate _rotateData;

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

            _rotateData = data.RotateData;

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
            if (!ProjectileRotation.TryApplySpin(_owner, _rotateData, tick))
                ProjectileRotation.Apply(_owner, position - previousPosition, _rotationOffset);

            if (!_isArrived && t >= 1f)
            {
                _isArrived = true;
                OnArrived?.Invoke();
            }

            if (!_isArrived)
                return;

            _isCompleted = true;
            OnExecute?.Invoke(new ProjectileImpact(null, _destination));
            OnExpired?.Invoke();
        }

        public void HitTarget(ICombatant target) { }

        public void Dispose()
        {
            _isCompleted = true;
            OnArrived = null;
            OnExecute = null;
            OnExpired = null;
        }
    }

    internal static class ProjectileRotation
    {
        private const float DirectionSqrEpsilon = 0.000001f;

        public static bool TryApplySpin(Transform projectile, ProjectileRotate rotateData, float deltaTime)
        {
            if (rotateData == null || rotateData.RotateType != ProjectileRotateType.Rotate)
                return false;

            projectile.Rotate(0f, 0f, rotateData.RotateSpeed * deltaTime, Space.Self);
            return true;
        }

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
