using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EMoveDirection
{
    None,
    Up,
    Right,
    Down,
    Left
}

//TODO Coroutine 말고 Update에서 돌리기
public class MoveController : MonoBehaviour
{
    private Transform _owner;
    private float _speed;
    private Coroutine _moveCoroutine;
    private EMoveDirection _moveDirection = EMoveDirection.None;

    public event Action OnArrivedDestination;
    public event Action<EMoveDirection> OnDirectionChanged;

    public void Init(Transform owner, float speed)
    {
        _owner = owner;
        _speed = speed;
    }

    public void MoveToDestination(Vector3 destination)
    {
        StopMove();
        SetMoveDirection(destination);
        _moveCoroutine = StartCoroutine(CoMoveToDestination(destination));
    }

    private void SetMoveDirection(Vector3 destination)
    {
        float xDiff = destination.x - this.transform.position.x;
        float yDiff = destination.y - this.transform.position.y;

        if (xDiff > 0)
            _moveDirection = EMoveDirection.Right;
        else if(xDiff < 0)
            _moveDirection = EMoveDirection.Left;
        else if(yDiff > 0)
            _moveDirection = EMoveDirection.Up;
        else
            _moveDirection = EMoveDirection.Down;

        OnDirectionChanged?.Invoke(_moveDirection);
    }

    private void StopMove()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null; 
        }
    }

    IEnumerator CoMoveToDestination(Vector3 destination)
    {
        Vector3 moveDir = destination - this.transform.position;
        moveDir = moveDir.normalized;

        while (true)
        {
            _owner.position += moveDir * _speed * Time.deltaTime;

            if (IsArrived(destination))
            {
                SetPosition(destination);
                break;
            }

            yield return null;
        }

        OnArrivedDestination?.Invoke();
    }
    private void SetPosition(Vector3 target)
    {
        _owner.position = target;
    }

    private bool IsArrived(Vector3 destination)
    {
        Vector3 remainVector = destination - this.transform.position;
        float remainDistance = Vector3.SqrMagnitude(remainVector);

        return remainDistance <= 0.001f;
    }
}
