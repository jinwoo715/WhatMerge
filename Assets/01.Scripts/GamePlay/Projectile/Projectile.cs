using Combat;
using Skill;
using System;
using UnityEngine;
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

    private bool _isArrived;

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

        _isArrived = false;

        _lifeTime = 0;
    }

    private void Update()
    {
        if (IsActive == false) return;

        if(_lifeTime >= _projectileData.LifeTime)
        {
            _destroyer.SetTrigget(EProjectileTrigger.TimeOut);
            _effectResolver.SetTrigget(EProjectileTrigger.TimeOut);
            return;
        }

        _lifeTime += Time.deltaTime;

        if(_target == null && _moveStretagy is HomingMove)
        {
            Return();
            return;
        }

        _destroyer.SetTrigget(EProjectileTrigger.Continue);
        _effectResolver.SetTrigget(EProjectileTrigger.Continue);

        _moveStretagy.OnMove();

        if (_isArrived) return;

        if (_moveStretagy.IsArrived(this.transform, _target))
        {
            _effectResolver.SetTrigget(EProjectileTrigger.Arrived);
            _destroyer.SetTrigget(EProjectileTrigger.Arrived);
            _isArrived = true;
        }
    }

    public void Return()
    {
        OnReturn?.Invoke(this);
    }
    public void ExcuteEffect()
    {
        _effectResolver.Resolve(_data, this.transform.position);
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

    private bool _isArrived;

    public void Init(Transform owner, ICreature target, float speed)
    {
        _owner = owner;
        _target = target;
        _speed = speed;
        _isArrived = false;
    }
    public void OnMove()
    {
        if (_isArrived)
        {
            _owner.position = _target.Position;
            return;
        }
        else
        {
            Vector3 dir = (_target.Position - _owner.position).normalized;
            _owner.position += dir * Time.deltaTime * _speed;

            RotationToTarget(dir);
        }
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

        _isArrived = distance <= 0.001f;

        return _isArrived;
    }

}
public class Parabola : IMoveStretagy
{
    private Transform _owner;
    private Vector3 _startPosition;
    private Vector3 _destination;
    private float _progress;
    private float _speed;
    public void Init(Transform owner, ICreature target, float speed)
    {
        _owner = owner;
        _startPosition = owner.position;
        _destination = target.Position;
        _speed = speed;
        _progress = 0;
    }

    public bool IsArrived(Transform t, ICreature target)
    {
        float distance = Vector3.SqrMagnitude(_owner.position - _destination);
        return distance <= 0.001f;
    }

    public void OnMove()
    {
        _progress += Time.deltaTime * _speed;
        float t = Mathf.Clamp01(_progress);

        // 1. 기본 직선 보간
        Vector3 pos = Vector3.Lerp(_startPosition, _destination, t);

        // 2. 포물선(y) 추가 (sin 기반 아크)
        float height = Mathf.Sin(t * Mathf.PI) * 0.9f;
        pos.y += height;

        _owner.position = pos;
    }
}
#endregion

public interface ICollider
{
    event Action<IDamageable> OnColliderEnter;
    void CheckCollider();
}

public class ProjectileDestoryer : IProjectileDestroyer
{
    public event Action OnDestory;
    public EProjectileTrigger _destroyType;
    private int _value;
    public void Init(EProjectileTrigger destroyType, int value)
    {
        Debug.Log(destroyType);
        _destroyType = destroyType;
        _value = value;
    }

    public void SetTrigget(EProjectileTrigger projectileTrigger)
    {
        if(_destroyType == projectileTrigger)
        {
            if(_destroyType == EProjectileTrigger.Continue)
            {

            }
            else
            {
                OnDestory?.Invoke();
                Debug.Log("Destory");
            }
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
    public void Init(EProjectileTrigger destroyType, int value);
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
    void Init(EProjectileTrigger type);
    void SetTrigget(EProjectileTrigger projectileTrigger);
    public void Resolve(ProjectilePayload data, Vector3 destination);
}

public class SingleDamageResolver : IProjectileEffectResolver
{
    public event Action OnHitResolver;
    private EProjectileTrigger _triggerType;

    public void Init(EProjectileTrigger type)
    {
        _triggerType = type;
    }

    public void Resolve(ProjectilePayload data, Vector3 destination)
    {
        Debug.Log("Resolve");
        float dmgMultiple = data.DMGValue * 0.01f;
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
        if (projectileTrigger == _triggerType)
        {
            OnHitResolver?.Invoke();
        }
    }
}
public class AreaDamageResolver : IProjectileEffectResolver
{
    public event Action OnHitResolver;
    private EProjectileTrigger _triggerType;

    public void Init(EProjectileTrigger type)
    {
        _triggerType = type;
    }
    public void Resolve(ProjectilePayload data, Vector3 destination)
    {
        var enemies = CreatureFinder.TryFindNearEnemies(destination, 0.5f);

        float dmgMultiple = data.DMGValue * 0.01f;
        float damage = data.attackStatProvider.GetStat(EAttackStatType.Damage) * dmgMultiple;

        int resultDamage = Mathf.RoundToInt(damage);

        int FlatPenetration = (int)data.attackStatProvider.GetStat(EAttackStatType.FlatPentration);
        int PercentPenetration = (int)data.attackStatProvider.GetStat(EAttackStatType.PercentPenetration);

        data.attackRegister.RegisterAttack(new DamageContext(data.VFX, destination, data.Attacker));

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackPayload ap = new AttackPayload(resultDamage, FlatPenetration, PercentPenetration);
            DamageContext dc = new DamageContext(ap, enemies[i], string.Empty, data.Attacker);
            data.attackRegister.RegisterAttack(dc);
        }
    }

    public void SetTrigget(EProjectileTrigger projectileTrigger)
    {
        if (projectileTrigger == _triggerType)
        {
            OnHitResolver?.Invoke();
        }
    }
}

