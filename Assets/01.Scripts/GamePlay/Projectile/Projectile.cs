using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Projectile

// 이동, 생명주기, Effect


public class Projectile : MonoBehaviour, IPooledItem<Projectile>
{
    [SerializeField] private SpriteRenderer _renderer;

    private float _lifeTime;

    private IMoveStretagy _moveStretagy;
    private IProjectileDestroyer _destroyer;
    private IProjectileEffectResolver _effectResolver;

    private ProjectileData _projectileData;
    private ProjectilePayload _data;

    private ICreature _target;
    private float _speed;

    public bool IsActive { get; private set; }

    public event Action<Projectile> OnReturn;

    public void Init(ProjectilePayload data, IMoveStretagy moveStretagy, IProjectileDestroyer collision, IProjectileEffectResolver effectResolver, ProjectileData projectileData, Sprite sprite, ICreature target)
    {
        _data = data;
        _moveStretagy = moveStretagy;
        _destroyer = collision;
        _effectResolver = effectResolver;
        _projectileData = projectileData;

        _destroyer.OnDestory += Return;
        _effectResolver.OnHitResolver += ExcuteEffect;

        _target = target;
        _moveStretagy.Init(this.transform, _target, projectileData.Speed);

        _renderer.sprite = sprite;

        _lifeTime = 0;
    }

    private void Update()
    {
        if (IsActive == false) return;

        if(_lifeTime >= 3.0)
        {
            _destroyer.SetTrigget(EProjectileTrigger.TimeOut);
            _effectResolver.SetTrigget(EProjectileTrigger.TimeOut);
            OnReturn?.Invoke(this);
            return;
        }

        _lifeTime += Time.deltaTime;

        if (_target == null)
        {
            OnReturn?.Invoke(this);
            return;
        }

        _moveStretagy.OnMove();

        _destroyer.SetTrigget(EProjectileTrigger.Continue);
        _effectResolver.SetTrigget(EProjectileTrigger.Continue);

        if (_moveStretagy.IsArrived(this.transform, _target))
        {
            _effectResolver.SetTrigget(EProjectileTrigger.Arrived);
            _destroyer.SetTrigget(EProjectileTrigger.Arrived);
        }
    }

    public void Return()
    {
        OnReturn?.Invoke(this);
    }
    public void ExcuteEffect()
    {
        _effectResolver.Resolve(_data);
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

public interface ICollider
{
    event Action<IDamageable> OnColliderEnter;
    void CheckCollider();
}

public class ArriveDestory : IProjectileDestroyer
{
    public event Action OnDestory;
    public void SetTrigget(EProjectileTrigger projectileTrigger)
    {
        if(projectileTrigger == EProjectileTrigger.Arrived)
        {
            OnDestory?.Invoke();
        }
    }
}

public interface IMoveStretagy
{
    void Init(Transform owner, ICreature target, float speed);
    void OnMove();
    bool IsArrived(Transform t, ICreature target);
}
public interface IProjectileDestroyer
{
    event Action OnDestory;
    void SetTrigget(EProjectileTrigger projectileTrigger);
}
public enum EProjectileTrigger
{
    Continue,
    Arrived,
    TimeOut,
}


//데미지 주기, 소환물 소환

//데미지는 단일 or 범위
public interface IProjectileEffectResolver
{
    public event Action OnHitResolver;
    void SetTrigget(EProjectileTrigger projectileTrigger);
    public void Resolve(ProjectilePayload data);
}

public class SingleDamageResolver : IProjectileEffectResolver
{
    public event Action OnHitResolver;

    public void Resolve(ProjectilePayload data)
    {
        Debug.Log("Resolve");
        float dmgMultiple = data.Value * 0.01f;
        float damage = data.attackStatProvider.GetStat(EAttackStatType.Damage) * dmgMultiple;

        int resultDamage = Mathf.RoundToInt(damage);

        int FlatPenetration = (int)data.attackStatProvider.GetStat(EAttackStatType.FlatPentration);
        int PercentPenetration = (int)data.attackStatProvider.GetStat(EAttackStatType.PercentPenetration);

        AttackPayload ap = new AttackPayload(resultDamage, FlatPenetration, PercentPenetration);
        DamageContext dc = new DamageContext(ap, data.Target, data.VFX, data.Attacker);

        data.attackRegister.RegisterAttack(dc);
    }

    public void SetTrigget(EProjectileTrigger projectileTrigger)
    {
        if (projectileTrigger == EProjectileTrigger.Arrived)
        {
            OnHitResolver?.Invoke();
        }
    }
}

