using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;

public interface IStatusEffect
{
    bool IsExpired { get; }
    void Update(float deltaTime);
    void Apply();
    void Release();
}

//시간
public class Stun : IStatusEffect
{
    public float _remainTime;
    private IMoveable _moveable;
    public bool IsExpired { get; private set; }

    public Stun(float duration, IDamageable damageable)
    {
        _remainTime = duration;
        IsExpired = false;
        _moveable = damageable.Move;
    }

    public void UpdateStun(float duration)
    {
        _remainTime = duration;
    }

    public void Update(float deltaTime)
    {
        _remainTime -= deltaTime;

        if (_remainTime <= 0)
            IsExpired = true;
    }

    public void Apply()
    {
        _moveable.StunOn();
    }

    public void Release()
    {
        _moveable.StunOff();
    }
}

//시간
//수치
public class Slow : IStatusEffect
{
    private readonly IEnemyStatModifier _enemyStatModifier;

    public float RemainTime { get; private set; }
    public float SlowValue { get; private set; }
    public bool IsExpired { get; set; }

    public Slow(float duration, float value, IDamageable target)
    {
        RemainTime = duration;
        SlowValue = value;
        IsExpired = false;

        _enemyStatModifier = target.StatModifier;
    }
    public void Update(float duration, float value)
    {
        RemainTime = duration;
        SlowValue = value;
    }

    public void Update(float deltaTime)
    {
        RemainTime -= deltaTime;

        if (RemainTime <= 0)
            IsExpired = true;
    }

    public void Apply()
    {
        _enemyStatModifier.AddMultiplier(EnemyStatType.MoveSpeed, -SlowValue);
    }

    public void Release()
    {
        _enemyStatModifier.AddMultiplier(EnemyStatType.MoveSpeed, SlowValue);
    }
}

//시간
//수치
public class ArmorReduction : IStatusEffect
{
    private readonly IEnemyStatModifier _enemyStatModifier;

    public float RemainTime { get; private set; }
    public float ReductionValue { get; private set; }
    public bool IsExpired { get; set; }

    public ArmorReduction(float duration, float value, IDamageable target)
    {
        RemainTime = duration;
        ReductionValue = value;
        IsExpired = false;

        _enemyStatModifier = target.StatModifier;
    }
    public void Update(float duration, float value)
    {
        RemainTime = duration;
        ReductionValue = value;
    }

    public void Update(float deltaTime)
    {
        RemainTime -= deltaTime;

        if (RemainTime <= 0)
            IsExpired = true;
    }

    public void Apply()
    {
        _enemyStatModifier.AddMultiplier(EnemyStatType.Armor, -ReductionValue);
    }

    public void Release()
    {
        _enemyStatModifier.AddMultiplier(EnemyStatType.Armor, ReductionValue);
    }
}

//시간
//속성
public class Element : IStatusEffect
{
    private float _duration;
    private ElementType _elementType;
    private IElement _element;
    public bool IsExpired { get; private set; }

    public Element(float duration, ElementType elementType, IElement element)
    {
        _duration = duration;
        _elementType = elementType;
        _element = element;
    }
    public void Apply()
    {
        _element.GetElement(_elementType);
    }
    public void Release()
    {
        _element.ReleaseElement(_elementType);
    }
    public void Update(float deltaTime)
    {
        _duration -= deltaTime;

        if(_duration <= 0)
        {
            IsExpired = true;
        }
    }
    public void UpdateTime(float duration)
    {
        _duration = Mathf.Max(duration, _duration);
    }
}

public class DamageTransfer : IStatusEffect
{
    public bool IsExpired { get; private set; }

    public readonly IDamageable Target;
    public float Duration;
    public readonly float TransferRange;
    public readonly float TransferValue;
    public readonly int TransferUnitCount;
    public readonly IDamageApplier DamageApplier;

    public DamageTransfer(IDamageApplier damageApplier, IDamageable damageable, float duration, 
        float transferRange, float transferValue, int transferUnitCount)
    {
        Target = damageable;

        Duration = duration;
        TransferRange = transferRange;
        TransferValue = transferValue;
        TransferUnitCount = transferUnitCount;
        DamageApplier = damageApplier;
    }

    public void Apply()
    {
        Target.OnAppliedNomalDamage += TransferDamage;
    }

    public void Release()
    {
        Target.OnAppliedNomalDamage -= TransferDamage;
    }

    public void Update(float deltaTime)
    {
        Duration -= deltaTime;

        if (Duration <= 0)
        {
            IsExpired = true;
        }
    }

    private void TransferDamage(int damage)
    {
        if (Target is not Enemy enemy)
            return;

        var enemies = SearchUtility.GetNearEnemiesByDistance(Target.Position, TransferRange, TransferUnitCount, enemy);

        if (enemies == null || enemies.Count == 0)
            return;

        int transferDamage = Mathf.RoundToInt(damage * TransferValue);

        foreach (var target in enemies)
        {
            DamageApplier.TryApply(target, transferDamage, DamageResultType.TransferDamage);
        }
    }
}

public interface ITimeEffectService
{
    void ApplySlow(float duration, float slowEffect, ICombatant target);
    void ApplyArmorReduction(float duration, float reduction, ICombatant target);
    void ApplyStun(float duration, ICombatant target);
    void ApplyElement(float duration, ICombatant combatant, ElementType type);
    void ApplyDamageTransfer(IDamageApplier damageApplier, float duration, ICombatant damageable, DamageTransferEffect effect);
}

public class TimeEffectManager : MonoBehaviour, ITimeEffectService
{
    private Dictionary<ICombatant, Slow> _slows = new();
    private Dictionary<ICombatant, Stun> _stuns = new();
    private Dictionary<ICombatant, ArmorReduction> _armorReductions = new();
    private Dictionary<(ICombatant, ElementType), Element> _elements = new();
    private Dictionary<IDamageable, DamageTransfer> _damageTransfers = new();

    public void ApplySlow(float duration, float slowValue, ICombatant target)
    {
        if(target is IDamageable damageable)
        {
            if(_slows.TryGetValue(target, out Slow value))
            {
                if (value.SlowValue > slowValue)
                    return;
                else
                    value.Update(duration, slowValue);
            }
            else
            {
                Slow slow = new Slow(duration, slowValue, damageable);
                slow.Apply();
                _slows.Add(target, slow);
            }
        }
    }
    public void ApplyArmorReduction(float duration, float reduction, ICombatant target)
    {
        if (target is IDamageable damageable)
        {
            if (_armorReductions.TryGetValue(target, out ArmorReduction value))
            {
                if (value.ReductionValue > reduction)
                    return;
                else
                    value.Update(duration, reduction);
            }
            else
            {
                ArmorReduction armorReduction = new ArmorReduction(duration, reduction, damageable);
                armorReduction.Apply();
                _armorReductions.Add(target, armorReduction);
            }
        }
    }
    public void ApplyStun(float duration, ICombatant target)
    {
        if(target is IDamageable damageable)
        {
            if(_stuns.TryGetValue(target, out Stun stun))
                stun.UpdateStun(duration);
            else
            {
                Stun newStun = new Stun(duration, damageable);
                newStun.Apply();
                _stuns.Add(target, newStun);
            }
        }
    }
    public void ApplyElement(float duration, ICombatant combatant, ElementType type)
    {
        if(_elements.TryGetValue((combatant, type), out var value))
        {
            value.UpdateTime(duration);
        }
        else
        {
            Element element = new Element(duration, type, combatant.Element);
            element.Apply();
            _elements.Add((combatant, type), element);
        }
    }
    public void ApplyDamageTransfer(IDamageApplier damageApplier, float duration, ICombatant combatant, DamageTransferEffect effect)
    {
        if (combatant is not IDamageable damageable)
            return;

        if(_damageTransfers.TryGetValue(damageable, out var value))
        {
            //기존 수치가 더 크면 교체
            //기존 수치와 같고 시간이 더 길면 교체

            if(effect.TransitionRatio > value.TransferValue)
            {
                _damageTransfers[damageable].Release();
                _damageTransfers[damageable] = new DamageTransfer(damageApplier, damageable, duration, effect.Radius, effect.TransitionRatio, effect.Count);
                _damageTransfers[damageable].Apply();
            }
            else if(effect.TransitionRatio == value.TransferValue && duration > value.Duration)
            {
                _damageTransfers[damageable].Release();
                _damageTransfers[damageable] = new DamageTransfer(damageApplier, damageable, duration, effect.Radius, effect.TransitionRatio, effect.Count);
                _damageTransfers[damageable].Apply();
            }
        }
        else
        {
            _damageTransfers.Add(damageable, new DamageTransfer(damageApplier, damageable, duration, effect.Radius, effect.TransitionRatio, effect.Count));
            _damageTransfers[damageable].Apply();
            damageable.OnActiveOff += RemoveAllDamageTransfer;
        }
    }

    private void RemoveAllDamageTransfer(ICombatant combatant)
    {
        if(combatant is IDamageable damageable)
        {
            if (_damageTransfers.ContainsKey(damageable))
            {
                _damageTransfers[damageable].Release();
                _damageTransfers.Remove(damageable);
            }
            combatant.OnActiveOff -= RemoveAllDamageTransfer;
        }
    }

    private void Update()
    {
        UpdateSlows();
        UpdateStun();
        UpdateArmorReductions();
        UpdateElement();
        UpdateDamageTransfer();
    }

    private void UpdateDamageTransfer()
    {
        List<IDamageable> keys = new List<IDamageable>();

        foreach (var transfer in _damageTransfers)
        {
            transfer.Value.Update(Time.deltaTime);

            if (transfer.Value.IsExpired)
            {
                transfer.Value.Release();
                keys.Add(transfer.Key);
            }
        }

        for (int i = 0; i < keys.Count; i++)
        {
            keys[i].OnActiveOff -= RemoveAllDamageTransfer;
            _damageTransfers.Remove(keys[i]);
        }
    }
    private void UpdateElement()
    {
        List<(ICombatant, ElementType)> keys = new List<(ICombatant, ElementType)>();

        foreach (var element in _elements)
        {
            element.Value.Update(Time.deltaTime);

            if (element.Value.IsExpired)
            {
                element.Value.Release();
                keys.Add(element.Key);
            }
        }

        foreach (var key in keys)
        {
            _elements.Remove(key);
        }
    }
    private void UpdateStun()
    {
        List<ICombatant> keys = new List<ICombatant>();

        foreach (var stun in _stuns)
        {
            stun.Value.Update(Time.deltaTime);

            if (stun.Value.IsExpired)
            {
                stun.Value.Release();
                keys.Add(stun.Key);
            }
        }

        foreach (var key in keys)
        {
            _stuns.Remove(key);
        }
    }
    private void UpdateSlows()
    {
        List<ICombatant> keys = new List<ICombatant>();

        foreach (var slow in _slows)
        {
            slow.Value.Update(Time.deltaTime);

            if (slow.Value.IsExpired)
            {
                slow.Value.Release();
                keys.Add(slow.Key);
            }
        }

        foreach (var key in keys)
        {
            _slows.Remove(key);
        }
    }
    private void UpdateArmorReductions()
    {
        List<ICombatant> keys = new List<ICombatant>();

        foreach (var reduction in _armorReductions)
        {
            reduction.Value.Update(Time.deltaTime);

            if (reduction.Value.IsExpired)
            {
                reduction.Value.Release();
                keys.Add(reduction.Key);
            }
        }

        foreach (var key in keys)
        {
            _armorReductions.Remove(key);
        }
    }
}
