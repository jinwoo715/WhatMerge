using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonData
{
    public int UID;
    public string Sprite;
    public float LifeTime;
    public ESommonPosType PivotPosType;
    public ESummonMoveType MoveType;
    public ESummonExcuteType HitType;
    public float Delay;      // OnDelay용
    public float Interval;   // OnInterval용
    public float Radius;
}
public enum ESummonExcuteType
{
    Once,
    Interval
}
public enum ESummonMoveType
{
    Fix,
    Follow,
    Approach,
}
public enum ESommonPosType
{
    Center,
    Left,
    Right,
    Upper,
    Bottom
}

//이동
//Effect

public interface ISummonMove
{
    void Init(ICreature target, Transform owner);
    void Move(ICreature target, Transform owner);
}
public class FixSummon : ISummonMove
{
    public void Init(ICreature target, Transform owner)
    {
        
    }

    public void Move(ICreature target, Transform owner)
    {
        
    }
}
public class FollowSummon : ISummonMove
{
    Vector3 _deltaPosition;
    public void Init(ICreature target, Transform owner)
    {
        _deltaPosition = target.Position;
    }

    public void Move(ICreature target, Transform owner)
    {
        Vector3 move = target.Position - _deltaPosition;
        _deltaPosition = target.Position;
        owner.position += move;
    }
}
public class ApprochSummon : ISummonMove
{
    Vector3 _deltaPosition;
    Vector3 _approchDir;
    public void Init(ICreature target, Transform owner)
    {
        _deltaPosition = target.Position;
        _approchDir = (target.Position - owner.position).normalized;
    }

    public void Move(ICreature target, Transform owner)
    {
        Vector3 move = target.Position - _deltaPosition;
        _deltaPosition = target.Position;
        owner.position += move;
        owner.position += _approchDir * Time.deltaTime;
    }
}
public interface ISummonEffect
{
    event Action OnEffect;
    void Init(float interval, float delay);
    void Tick();
}
public class SummonOnceEffect : ISummonEffect
{
    private float _delay;
    private float _currentTime;

    public event Action OnEffect;
    public void Init(float interval, float delay) 
    {
        _delay = delay;
        _currentTime = 0;
    }
    public void Tick()
    {
        Debug.Log($"{_currentTime}, {_delay}");

        if (_currentTime >= _delay)
            return;

        _currentTime += Time.deltaTime;

        Debug.Log($"{_currentTime}, {_delay}");

        if (_currentTime >= _delay)
            OnEffect?.Invoke();
    }
}
public class SummonIntervalEffect : ISummonEffect
{
    private float _interval;
    private float _currentTime;

    public event Action OnEffect;
    public void Init(float interval, float delay)
    {
        _interval = interval;
    }
    public void Tick()
    {
        _currentTime += Time.deltaTime;

        if (_currentTime >= _interval)
        {
            _currentTime = 0;
            OnEffect?.Invoke();
        }
    }
}

public interface IHitEffect
{
    public void ExcuteEffect(ProjectilePayload data, Transform _owner, float radius);
}

public class MultiAttackEffect : IHitEffect
{
    public void ExcuteEffect(ProjectilePayload data, Transform _owner, float radius)
    {
        Debug.Log($"AOE Attack {radius}");

        var enemies = CreatureFinder.TryFindNearEnemies(_owner.position, radius);

        Debug.Log($"Find : {enemies}");

        float dmgMultiple = data.Value * 0.01f;
        float damage = data.attackStatProvider.GetStat(EAttackStatType.Damage) * dmgMultiple;

        int resultDamage = Mathf.RoundToInt(damage);

        int FlatPenetration = (int)data.attackStatProvider.GetStat(EAttackStatType.FlatPentration);
        int PercentPenetration = (int)data.attackStatProvider.GetStat(EAttackStatType.PercentPenetration);

        for (int i = 0; i < enemies.Count; i++)
        {
            AttackPayload ap = new AttackPayload(resultDamage, FlatPenetration, PercentPenetration);
            DamageContext dc = new DamageContext(ap, enemies[i], string.Empty, data.Attacker);
            data.attackRegister.RegisterAttack(dc);
        }
    }
}

public class Summon : MonoBehaviour, IPooledItem<Summon>
{
    [SerializeField] private SpriteRenderer _renderer;

    private SummonData _summonData;
    private ProjectilePayload _data;
    public bool IsActive { get; private set; } = true;

    public event Action<Summon> OnReturn;

    private float _lifeTime = 0;
    private float _intervalTime = 0;

    private ISummonMove _summonMove;
    private ISummonEffect _summonEffect;
    private IHitEffect _effectResolver;

    public void Init(ProjectilePayload data, SummonData summonData, Sprite sprite, ISummonMove summonMove, ISummonEffect summonEffect, IHitEffect hitEffect)
    {
        _data = data;
        _summonData = summonData;
        _renderer.sprite = sprite;
        _lifeTime = summonData.LifeTime;
        _intervalTime = summonData.Interval;
        _summonMove = summonMove;
        _summonEffect = summonEffect;

        summonEffect.Init(summonData.Interval, summonData.Delay);

        _effectResolver = hitEffect;
        _summonEffect.OnEffect += OnExcuteEffect;
    }

    private void OnExcuteEffect()
    {
        _effectResolver.ExcuteEffect(_data, this.transform, _summonData.Radius);
    }

    private void Update()
    {
        Debug.Log("????");
        if (!IsActive) return;

        _lifeTime -= Time.deltaTime;

        Debug.Log("!!!!");
        if (_lifeTime < 0)
            OnReturn?.Invoke(this);

        Debug.Log($"!@#!@#!@# : {_summonEffect}");
        _summonMove.Move(_data.Target, this.transform);
        _summonEffect.Tick();
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
