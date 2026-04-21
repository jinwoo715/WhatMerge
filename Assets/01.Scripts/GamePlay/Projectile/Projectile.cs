using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IPooledItem<Projectile>
{
    [SerializeField] private SpriteRenderer _renderer;

    private float _lifeTime;

    private IMoveStretagy _moveStretagy;
    private ICollision _excuteStrategy;

    private ProjectileData _projectileData;

    private ICreature _target;
    private float _speed;

    public bool IsActive { get; private set; }

    public event Action<Projectile> OnReturn;

    public void Init(IMoveStretagy moveStretagy, ICollision excuteStrategy, ProjectileData projectileData, Sprite sprite, ICreature target)
    {
        _moveStretagy = moveStretagy;
        _excuteStrategy = excuteStrategy;
        _projectileData = projectileData;

        _target = target;
        _moveStretagy.Init(this.transform, _target, _speed);

        _renderer.sprite = sprite;

        _lifeTime = 0;
    }

    private void Update()
    {
        if (IsActive == false) return;

        if(_lifeTime >= 3.0)
        {
            OnReturn?.Invoke(this);
            return;
        }

        _lifeTime += Time.deltaTime;

        Debug.Log($"{_lifeTime}, {_target}");

        if (_target == null)
        {
            OnReturn?.Invoke(this);
            return;
        }

        _moveStretagy.OnMove();

        if (_moveStretagy.IsArrived(this.transform, _target))
        {
            _excuteStrategy.OnHit();
        }
    }
    public void OnDespawn()
    {
        IsActive = false;
    }
    public void OnSpawn()
    {
        IsActive = true;
    }
}

//이동만
#region Move
public class LinearMove : IMoveStretagy
{
    private Transform _owner;
    private Vector3 _dir;
    private float _speed;
    private float _lifeTime;

    public void Init(Transform owner, ICreature target, float speed)
    {
        _lifeTime = 3.0f;

        _dir = (target.Position - owner.position).normalized;
        _speed = speed;
        _owner = owner;
    }

    public void OnMove()
    {
        _lifeTime -= Time.deltaTime;

        _owner.position += _dir * Time.deltaTime * _speed;
    }

    public bool IsArrived(Transform t, ICreature target)
    {
        return _lifeTime < 0;
    }
}
public class HomingMove : IMoveStretagy
{
    private Transform _owner;
    ICreature _target;
    private float _speed;

    public void Init(Transform owner, ICreature target, float speed)
    {
        _owner = owner;
        _target = target;
        _speed = speed;
    }
    public void OnMove()
    {
        Vector3 dir = (_target.Position - _owner.position).normalized;
        _owner.position += dir * Time.deltaTime * _speed;

        RotationToTarget(dir);
    }

    private void RotationToTarget(Vector3 dir)
    {
        float angleRad = Mathf.Atan2(dir.y, dir.x);
        float angleDeg = angleRad * Mathf.Rad2Deg - 90f;

        Quaternion targetRotation = Quaternion.Euler(0, 0, angleDeg);

        _owner.rotation = targetRotation;
    }

    public bool IsArrived(Transform t, ICreature target)
    {
        float distance = Vector3.SqrMagnitude(_owner.position - target.Position);
        return distance <= 0.001f;
    }

}
public class Parabola : IMoveStretagy
{
    public void Init(Transform owner, ICreature target, float speed)
    {

    }

    public bool IsArrived(Transform t, ICreature target)
    {
        return false;
    }

    public void OnMove()
    {

    }
}
#endregion

#region Excute
public class ProjectileHit : ICollision
{
    public void Init(ICreature target, int summonUid, int p1, int p2)
    {
        throw new NotImplementedException();
    }

    public void OnHit()
    {
        throw new NotImplementedException();
    }
}

//때리고 뭔가 소환함
public class SummonHit : ICollision
{
    public void Init(ICreature target, int summonUid, int p1, int p2)
    {
        throw new NotImplementedException();
    }

    public void OnHit()
    {
        throw new NotImplementedException();
    }
}
#endregion

public class ProjectileCollision
{

}

public interface IMoveStretagy
{
    void Init(Transform owner, ICreature target, float speed);
    void OnMove();
    bool IsArrived(Transform t, ICreature target);
}
public interface ICollision
{
    void Init(ICreature target, int summonUid, int p1, int p2);
    void OnHit();
}

public interface IHitResolver
{
    void Init(ICreature target);
    void Resolve();
}
