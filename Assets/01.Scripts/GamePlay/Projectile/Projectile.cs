using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//HeroData


//이동
//종료
//처리

// 모든 스킬에 기본적으로 필요한 것

// 이름, 설명, 계수, 
//MeleeSkill 
//ProjectileSkill
//SummonSkill
//BuffSkill


public abstract class Projectile : MonoBehaviour, IPooledItem<Projectile>
{
    private IMoveStretagy _moveStretagy;
    private IHitEffect _excuteStrategy;

    private ProjectileData _projectileData;

    private ICreature _target;
    private float _speed;

    public bool IsActive { get; private set; }

    public event Action<Projectile> OnReturn;

    public void Init(IMoveStretagy moveStretagy, IHitEffect excuteStrategy, ProjectileData projectileData)
    {
        _moveStretagy = moveStretagy;
        _excuteStrategy = excuteStrategy;
        _projectileData = projectileData;
    }

    public void InitGuidedProjectile(ICreature target, float speed, float duration, int summonUID)
    {
        _target = target;
        _speed = speed;

        _moveStretagy.Init(this.transform, _target, _speed);
    }
    private void Update()
    {
        if (IsActive == false) return;

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
public class ProjectileHit : IHitEffect
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
public class SummonHit : IHitEffect
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
public interface IMoveStretagy
{
    void Init(Transform owner, ICreature target, float speed);
    void OnMove();
    bool IsArrived(Transform t, ICreature target);
}
public interface IHitEffect
{
    void Init(ICreature target, int summonUid, int p1, int p2);
    void OnHit();
}

public interface IHitResolver
{
    void Init(ICreature target);
    void Resolve();
}
