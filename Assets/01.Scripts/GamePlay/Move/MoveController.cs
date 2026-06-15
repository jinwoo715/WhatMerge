using Map;
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

public class MoveController : MonoBehaviour
{
    private Transform _owner;

    private bool _isMoveable = false;

    private float _speed;
    private int _moveIndex;
    private Vector3 _destination;
    private Vector3 _dir;
    
    private IPathProvider _provider;

    public event Action<EMoveDirection> OnDirectionChanged;

    private void Update()
    {
        if (!_isMoveable) return;

        MoveToDestination();
    }
    private void OnDisable()
    {
        _isMoveable = false;
    }
    public void Initialize(Transform owner, IPathProvider pathProvider)
    {
        _owner = owner;
        _provider = pathProvider;
    }
    public void Init(float speed)
    {
        _speed = speed;
        _moveIndex = 0;
        _destination = _provider.GetDestination(_moveIndex);
        _isMoveable = true;
    }

    public void UpdateSpeed(float speed)
    {
        _speed = speed;
    }

    public void MoveToDestination()
    {
        if (IsArrived())
        {
            SetPositionToDestination();
            UpdateDestination();
            UpdateMoveDirection();
        }

        _owner.transform.position += _dir * Time.deltaTime * _speed;
    }
    private bool IsArrived()
    {
        Vector3 remainVector = _destination - _owner.transform.position;
        float remainDistance = Vector3.SqrMagnitude(remainVector);

        return remainDistance <= 0.001f;
    }
    private void SetPositionToDestination()
    {
        _owner.position = _destination;
    }
    private void UpdateDestination()
    {
        _moveIndex = _provider.GetNextIndex(_moveIndex);
        _destination = _provider.GetDestination(_moveIndex);

        _dir = (_destination - _owner.transform.position).normalized;
    }
    private void UpdateMoveDirection()
    {
        EMoveDirection direction = EMoveDirection.None;

        float xDiff = _destination.x - this.transform.position.x;
        float yDiff = _destination.y - this.transform.position.y;

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
}
