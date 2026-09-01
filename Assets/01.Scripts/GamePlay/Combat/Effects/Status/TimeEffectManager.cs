using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;

namespace WhatMerge.Combat.Effects
{
public interface ITimeEffectService
{
    IRuntimeEffectHandle ApplySlow(float slowValue, ICombatant target);
    IRuntimeEffectHandle ApplyArmorReduction(float reductionValue, ICombatant target);
    IRuntimeEffectHandle ApplyStun(ICombatant target);
    IRuntimeEffectHandle ApplyElement(ICombatant target, ElementType type);
    IRuntimeEffectHandle ApplyDamageTransfer(
        IDamageApplier damageApplier,
        ICombatant target,
        DamageTransferEffect effect);
    void TrackDuration(IRuntimeEffectHandle handle, float duration, ICombatant target);
}

public class TimeEffectManager : MonoBehaviour, ITimeEffectService
{
    private sealed class TimedEffectEntry
    {
        public readonly IRuntimeEffectHandle Handle;
        public readonly ICombatant Target;
        public float RemainingTime;

        public TimedEffectEntry(IRuntimeEffectHandle handle, float duration, ICombatant target)
        {
            Handle = handle;
            Target = target;
            RemainingTime = duration;
        }
    }

    private sealed class StrongestStatGroup
    {
        private readonly IEnemyStatModifier _statModifier;
        private readonly EnemyStatType _statType;
        private readonly Dictionary<long, float> _values = new();
        private float _appliedValue;

        public int Count => _values.Count;

        public StrongestStatGroup(IEnemyStatModifier statModifier, EnemyStatType statType)
        {
            _statModifier = statModifier;
            _statType = statType;
        }

        public void Add(long token, float value)
        {
            _values.Add(token, value);
            Recalculate();
        }

        public void Remove(long token)
        {
            if (!_values.Remove(token))
                return;

            Recalculate();
        }

        public void ReleaseAll()
        {
            _values.Clear();
            Recalculate();
        }

        private void Recalculate()
        {
            float strongestValue = 0f;

            foreach (float value in _values.Values)
            {
                if (value > strongestValue)
                    strongestValue = value;
            }

            if (Mathf.Approximately(_appliedValue, strongestValue))
                return;

            _statModifier.AddMultiplier(_statType, _appliedValue - strongestValue);
            _appliedValue = strongestValue;
        }
    }

    private sealed class StunGroup
    {
        private readonly IMoveable _moveable;
        private readonly HashSet<long> _tokens = new();

        public int Count => _tokens.Count;

        public StunGroup(IMoveable moveable)
        {
            _moveable = moveable;
        }

        public void Add(long token)
        {
            if (!_tokens.Add(token))
                return;

            if (_tokens.Count == 1)
                _moveable.StunOn();
        }

        public void Remove(long token)
        {
            if (!_tokens.Remove(token))
                return;

            if (_tokens.Count == 0)
                _moveable.StunOff();
        }

        public void ReleaseAll()
        {
            if (_tokens.Count > 0)
                _moveable.StunOff();

            _tokens.Clear();
        }
    }

    private sealed class ElementGroup
    {
        private readonly IStatusModifier _statusModifier;
        private readonly ElementType _elementType;
        private readonly HashSet<long> _tokens = new();

        public int Count => _tokens.Count;

        public ElementGroup(IStatusModifier statusModifier, ElementType elementType)
        {
            _statusModifier = statusModifier;
            _elementType = elementType;
        }

        public bool Add(long token)
        {
            if (_tokens.Contains(token) || !_statusModifier.IsAddableStatus(_elementType))
                return false;

            _tokens.Add(token);
            _statusModifier.AddStatus(_elementType);
            return true;
        }

        public void Remove(long token)
        {
            if (!_tokens.Remove(token))
                return;

            _statusModifier.RemoveStatus(_elementType);
        }

        public void ReleaseAll()
        {
            foreach (long _ in _tokens)
                _statusModifier.RemoveStatus(_elementType);

            _tokens.Clear();
        }
    }

    private sealed class DamageTransferApplication
    {
        private readonly IDamageable _target;
        private readonly IDamageApplier _damageApplier;
        private readonly float _range;
        private readonly int _unitCount;
        private bool _isApplied;

        public float TransferRatio { get; }

        public DamageTransferApplication(
            IDamageApplier damageApplier,
            IDamageable target,
            DamageTransferEffect effect)
        {
            _damageApplier = damageApplier;
            _target = target;
            _range = effect.Radius;
            _unitCount = effect.Count;
            TransferRatio = effect.TransitionRatio;
        }

        public void Apply()
        {
            if (_isApplied)
                return;

            _isApplied = true;
            _target.OnAppliedNomalDamage += TransferDamage;
        }

        public void Release()
        {
            if (!_isApplied)
                return;

            _isApplied = false;
            _target.OnAppliedNomalDamage -= TransferDamage;
        }

        private void TransferDamage(int damage)
        {
            if (_target is not Enemy enemy)
                return;

            var enemies = SearchUtility.GetNearEnemiesByDistance(
                _target.Position,
                _range,
                _unitCount,
                enemy);

            if (enemies == null || enemies.Count == 0)
                return;

            int transferDamage = Mathf.RoundToInt(damage * TransferRatio);

            foreach (var target in enemies)
            {
                _damageApplier.TryApply(
                    target,
                    transferDamage,
                    DamageResultType.TransferDamage);
            }
        }
    }

    private sealed class DamageTransferGroup
    {
        private readonly Dictionary<long, DamageTransferApplication> _applications = new();
        private long? _activeToken;

        public int Count => _applications.Count;

        public void Add(long token, DamageTransferApplication application)
        {
            _applications.Add(token, application);
            Recalculate();
        }

        public void Remove(long token)
        {
            if (!_applications.ContainsKey(token))
                return;

            if (_activeToken == token)
            {
                _applications[token].Release();
                _activeToken = null;
            }

            _applications.Remove(token);
            Recalculate();
        }

        public void ReleaseAll()
        {
            if (_activeToken.HasValue)
                _applications[_activeToken.Value].Release();

            _activeToken = null;
            _applications.Clear();
        }

        private void Recalculate()
        {
            long? strongestToken = null;
            float strongestRatio = float.MinValue;

            foreach (var application in _applications)
            {
                if (application.Value.TransferRatio > strongestRatio
                    || application.Value.TransferRatio == strongestRatio
                    && (!strongestToken.HasValue || application.Key > strongestToken.Value))
                {
                    strongestToken = application.Key;
                    strongestRatio = application.Value.TransferRatio;
                }
            }

            if (_activeToken == strongestToken)
                return;

            if (_activeToken.HasValue)
                _applications[_activeToken.Value].Release();

            _activeToken = strongestToken;

            if (_activeToken.HasValue)
                _applications[_activeToken.Value].Apply();
        }
    }

    private readonly Dictionary<ICombatant, StrongestStatGroup> _slows = new();
    private readonly Dictionary<ICombatant, StrongestStatGroup> _armorReductions = new();
    private readonly Dictionary<ICombatant, StunGroup> _stuns = new();
    private readonly Dictionary<(ICombatant, ElementType), ElementGroup> _elements = new();
    private readonly Dictionary<IDamageable, DamageTransferGroup> _damageTransfers = new();
    private readonly List<TimedEffectEntry> _timedEffects = new();
    private readonly HashSet<ICombatant> _trackedTargets = new();

    private long _nextToken;
    private IFatalStopService _fatalStop;

    public void Init(IFatalStopService fatalStop)
    {
        _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));
    }

    public IRuntimeEffectHandle ApplySlow(float slowValue, ICombatant target)
    {
        ValidateNonNegative(slowValue, nameof(slowValue));

        if (target is not IDamageable damageable)
        {
            throw new InvalidOperationException(
                $"Slow effect requires an {nameof(IDamageable)} target. " +
                $"Received: {target?.GetType().Name ?? "null"}.");
        }

        if (!target.IsActive)
            return RuntimeEffectHandle.Empty;

        if (!_slows.TryGetValue(target, out StrongestStatGroup group))
        {
            group = new StrongestStatGroup(damageable.StatModifier, EnemyStatType.MoveSpeed);
            _slows.Add(target, group);
        }

        long token = NextToken();
        group.Add(token, slowValue);

        return new RuntimeEffectHandle(() => RemoveStatEffect(_slows, target, token));
    }

    public IRuntimeEffectHandle ApplyArmorReduction(float reductionValue, ICombatant target)
    {
        ValidateNonNegative(reductionValue, nameof(reductionValue));

        if (target is not IDamageable damageable)
        {
            throw new InvalidOperationException(
                $"Armor reduction effect requires an {nameof(IDamageable)} target. " +
                $"Received: {target?.GetType().Name ?? "null"}.");
        }

        if (!target.IsActive)
            return RuntimeEffectHandle.Empty;

        if (!_armorReductions.TryGetValue(target, out StrongestStatGroup group))
        {
            group = new StrongestStatGroup(damageable.StatModifier, EnemyStatType.Armor);
            _armorReductions.Add(target, group);
        }

        long token = NextToken();
        group.Add(token, reductionValue);

        return new RuntimeEffectHandle(() => RemoveStatEffect(_armorReductions, target, token));
    }

    public IRuntimeEffectHandle ApplyStun(ICombatant target)
    {
        if (target is not IDamageable damageable)
        {
            throw new InvalidOperationException(
                $"Stun effect requires an {nameof(IDamageable)} target. " +
                $"Received: {target?.GetType().Name ?? "null"}.");
        }

        if (!target.IsActive)
            return RuntimeEffectHandle.Empty;

        if (!_stuns.TryGetValue(target, out StunGroup group))
        {
            group = new StunGroup(damageable.Move);
            _stuns.Add(target, group);
        }

        long token = NextToken();
        group.Add(token);

        return new RuntimeEffectHandle(() =>
        {
            if (!_stuns.TryGetValue(target, out StunGroup currentGroup))
                return;

            currentGroup.Remove(token);

            if (currentGroup.Count == 0)
                _stuns.Remove(target);
        });
    }

    public IRuntimeEffectHandle ApplyElement(ICombatant target, ElementType type)
    {
        if (target == null || !target.IsActive || type == ElementType.None)
            return RuntimeEffectHandle.Empty;
        if (target is not IDamageable damageable)
        {
            throw new InvalidOperationException(
                $"Element effect requires an {nameof(IDamageable)} target. " +
                $"Received: {target.GetType().Name}.");
        }

        var key = (target, type);

        if (!_elements.TryGetValue(key, out ElementGroup group))
        {
            group = new ElementGroup(damageable.TemporaryAttributeModifier, type);
            _elements.Add(key, group);
        }

        long token = NextToken();
        if (!group.Add(token))
        {
            if (group.Count == 0)
                _elements.Remove(key);

            return RuntimeEffectHandle.Empty;
        }

        return new RuntimeEffectHandle(() =>
        {
            if (!_elements.TryGetValue(key, out ElementGroup currentGroup))
                return;

            currentGroup.Remove(token);

            if (currentGroup.Count == 0)
                _elements.Remove(key);
        });
    }

    public IRuntimeEffectHandle ApplyDamageTransfer(
        IDamageApplier damageApplier,
        ICombatant target,
        DamageTransferEffect effect)
    {
        if (damageApplier == null)
            throw new ArgumentNullException(nameof(damageApplier));
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));
        if (target is not IDamageable damageable)
        {
            throw new InvalidOperationException(
                $"Damage transfer effect requires an {nameof(IDamageable)} target. " +
                $"Received: {target?.GetType().Name ?? "null"}.");
        }

        if (!target.IsActive)
            return RuntimeEffectHandle.Empty;

        if (!_damageTransfers.TryGetValue(damageable, out DamageTransferGroup group))
        {
            group = new DamageTransferGroup();
            _damageTransfers.Add(damageable, group);
        }

        long token = NextToken();
        group.Add(token, new DamageTransferApplication(damageApplier, damageable, effect));

        return new RuntimeEffectHandle(() =>
        {
            if (!_damageTransfers.TryGetValue(damageable, out DamageTransferGroup currentGroup))
                return;

            currentGroup.Remove(token);

            if (currentGroup.Count == 0)
                _damageTransfers.Remove(damageable);
        });
    }

    public void TrackDuration(IRuntimeEffectHandle handle, float duration, ICombatant target)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));
        if (target == null)
        {
            handle.Dispose();
            throw new ArgumentNullException(nameof(target));
        }
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
        {
            handle.Dispose();
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be greater than zero.");
        }
        if (!target.IsActive)
        {
            handle.Dispose();
            return;
        }
        if (handle.IsDisposed)
            return;

        if (_trackedTargets.Add(target))
            target.OnActiveOff += ReleaseTargetEffects;

        _timedEffects.Add(new TimedEffectEntry(handle, duration, target));
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = _timedEffects.Count - 1; i >= 0; i--)
        {
            TimedEffectEntry entry = _timedEffects[i];
            try
            {
                entry.RemainingTime -= deltaTime;

                if (!entry.Handle.IsDisposed
                    && entry.RemainingTime > 0f
                    && entry.Target.IsActive)
                {
                    continue;
                }

                entry.Handle.Dispose();
                _timedEffects.RemoveAt(i);
                UntrackTargetIfUnused(entry.Target);
            }
            catch (Exception exception)
            {
                _timedEffects.RemoveAt(i);

                try
                {
                    entry.Handle.Dispose();
                    UntrackTargetIfUnused(entry.Target);
                }
                catch (Exception cleanupException)
                {
                    Debug.LogException(cleanupException);
                }

                _fatalStop?.FatalStop(exception, "Timed status effect update failed.");
                throw;
            }
        }
    }

    private void OnDisable()
    {
        Exception firstException = null;

        for (int i = _timedEffects.Count - 1; i >= 0; i--)
            TryCleanup(_timedEffects[i].Handle.Dispose, ref firstException);

        _timedEffects.Clear();

        foreach (ICombatant target in _trackedTargets)
            target.OnActiveOff -= ReleaseTargetEffects;

        _trackedTargets.Clear();

        foreach (StrongestStatGroup group in _slows.Values)
            TryCleanup(group.ReleaseAll, ref firstException);
        foreach (StrongestStatGroup group in _armorReductions.Values)
            TryCleanup(group.ReleaseAll, ref firstException);
        foreach (StunGroup group in _stuns.Values)
            TryCleanup(group.ReleaseAll, ref firstException);
        foreach (ElementGroup group in _elements.Values)
            TryCleanup(group.ReleaseAll, ref firstException);
        foreach (DamageTransferGroup group in _damageTransfers.Values)
            TryCleanup(group.ReleaseAll, ref firstException);

        _slows.Clear();
        _armorReductions.Clear();
        _stuns.Clear();
        _elements.Clear();
        _damageTransfers.Clear();

        if (firstException != null)
            Debug.LogException(firstException);
    }

    private void ReleaseTargetEffects(ICombatant target)
    {
        Exception firstException = null;

        for (int i = _timedEffects.Count - 1; i >= 0; i--)
        {
            TimedEffectEntry entry = _timedEffects[i];

            if (!ReferenceEquals(entry.Target, target))
                continue;

            _timedEffects.RemoveAt(i);
            TryCleanup(entry.Handle.Dispose, ref firstException);
        }

        target.OnActiveOff -= ReleaseTargetEffects;
        _trackedTargets.Remove(target);

        if (firstException != null)
        {
            _fatalStop?.FatalStop(firstException, "Timed status target cleanup failed.");
            throw firstException;
        }
    }

    private void UntrackTargetIfUnused(ICombatant target)
    {
        for (int i = 0; i < _timedEffects.Count; i++)
        {
            if (ReferenceEquals(_timedEffects[i].Target, target))
                return;
        }

        target.OnActiveOff -= ReleaseTargetEffects;
        _trackedTargets.Remove(target);
    }

    private static void RemoveStatEffect(
        Dictionary<ICombatant, StrongestStatGroup> groups,
        ICombatant target,
        long token)
    {
        if (!groups.TryGetValue(target, out StrongestStatGroup group))
            return;

        group.Remove(token);

        if (group.Count == 0)
            groups.Remove(target);
    }

    private long NextToken()
    {
        return ++_nextToken;
    }

    private static void ValidateNonNegative(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Effect value must be a finite, non-negative number.");
        }
    }

    private static void TryCleanup(Action cleanup, ref Exception firstException)
    {
        try
        {
            cleanup?.Invoke();
        }
        catch (Exception exception)
        {
            if (firstException == null)
                firstException = exception;
            else
                Debug.LogException(exception);
        }
    }
}
}
