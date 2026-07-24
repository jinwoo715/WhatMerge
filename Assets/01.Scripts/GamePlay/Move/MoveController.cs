using WhatMerge.Map;
using System;
using UnityEngine;

public enum EMoveDirection
{
    None,
    Up,
    Right,
    Down,
    Left
}

public interface IMoveable
{
    void UpdateSpeed(float speed);
    void StunOn();
    void StunOff();
    void Knockback(float distance);
}

public enum CrowdControlType
{
    None,
    Stun,
    Knockback
}

public class MoveController : IMoveable
{
    private static float KnockbackVelocity = 3;
    private static float WallHitDistanceMultiplier = 0.5f;

    private Transform _owner;

    private bool _isMoveable = false;
    private bool _isStun = false;
    private bool _isKnockback = false;
    private bool _hasHitWall = false;

    private float _speed;
    private int _moveIndex;
    private Vector3 _deltaDestination;
    private Vector3 _destination;

    private float _knockbackDistance;
    private float _totalDistance = 0;
    private float _currentDistance = 0;
    
    private IPathProvider _provider;

    public event Action<EMoveDirection> OnDirectionChanged;

    public void ActiveOn()
    {
        _isMoveable = true;
    }
    public void ActiveOff()
    {
        _isMoveable = false;
        _isStun = false;
        _isKnockback = false;
        _hasHitWall = false;
        _knockbackDistance = 0;
        _totalDistance = 0;
        _currentDistance = 0;
        _moveIndex = 0;
        _speed = 0; 
    }
    public MoveController(Transform owner, IPathProvider pathProvider)
    {
        _owner = owner;
        _provider = pathProvider;
    }
    public void Init(float speed)
    {
        _speed = speed;
        _moveIndex = 0;
        
        Vector3 start = _provider.GetDestination(_moveIndex++);
        Vector3 next = _provider.GetDestination(_moveIndex);
        _deltaDestination = start;
        _destination = next;

        _totalDistance = CalcuateProgress(start, next);

        _isMoveable = true;
    }
    public void UpdateDeltatime(float deltaTime)
    {
        if (!_isMoveable) return;

        float moveVelocity = deltaTime * _speed;

        if (_isStun)
            moveVelocity = 0;

        if (_isKnockback)
        {
            float knockbackMove = Mathf.Min(deltaTime * KnockbackVelocity, _knockbackDistance);

            moveVelocity = -knockbackMove;
            _knockbackDistance -= knockbackMove;
              
            if (_currentDistance + moveVelocity <= 0 && !_hasHitWall)
            {
                _knockbackDistance *= WallHitDistanceMultiplier;
                _hasHitWall = true;
            }

            if (_knockbackDistance <= 0)
            {
                _isKnockback = false;
                
            }
        }
        
        _currentDistance += moveVelocity;

        _currentDistance = Mathf.Max(0, _currentDistance);

        float progress = _currentDistance / _totalDistance;

        if (progress >= 1)
            UpdateDestination();
        else
            _owner.position = Vector3.Lerp(_deltaDestination, _destination, progress);
    }
    private float CalcuateProgress(Vector3 start, Vector3 end)
    {
        return (start - end).magnitude;
    }
    public void UpdateSpeed(float speed)
    {
        _speed = speed;
    }
    private void UpdateDestination()
    {
        _owner.position = _destination;

        _moveIndex = _provider.GetNextIndex(_moveIndex);

        _deltaDestination = _destination;
        _destination = _provider.GetDestination(_moveIndex);

        _currentDistance = 0;
        _totalDistance = CalcuateProgress(_deltaDestination, _destination);
        UpdateMoveDirection();
    }
    private void UpdateMoveDirection()
    {
        EMoveDirection direction = EMoveDirection.None;

        float xDiff = _destination.x - _deltaDestination.x;
        float yDiff = _destination.y - _deltaDestination.y;

        if (xDiff > 0)
            direction = EMoveDirection.Right;
        else if(xDiff < 0)
            direction = EMoveDirection.Left;
        else if(yDiff > 0)
            direction = EMoveDirection.Up;
        else
            direction = EMoveDirection.Down;

        OnDirectionChanged?.Invoke(direction);
    }
    public void StunOn()
    {
        _isStun = true;
    }
    public void StunOff()
    {
        _isStun = false;
    }
    public void Knockback(float distance)
    {
        if (distance <= 0)
            return;

        _isKnockback = true;
        _hasHitWall = false;
        _knockbackDistance = distance;
    }
}
