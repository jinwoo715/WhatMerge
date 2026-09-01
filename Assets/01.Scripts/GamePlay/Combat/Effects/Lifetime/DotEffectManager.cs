using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace WhatMerge.Combat.Effects
{
    public interface IDotService
    {
        IRuntimeEffectHandle ApplyDotEffect(DotData dotData);
    }

    public class DotProcessBundle
    {
        private readonly Dictionary<long, DotProcess> _dotProcesses = new();

        public int Dots => _dotProcesses.Count;
        public List<DotProcess> AllProcesses => new(_dotProcesses.Values);

        public bool TryGetProcess(DotData key, out DotProcess value)
        {
            return _dotProcesses.TryGetValue(key.Key, out value);
        }

        public void AddDotProcess(DotData key, DotProcess value)
        {
            _dotProcesses.Add(key.Key, value);
        }

        public bool Contains(DotData data)
        {
            return _dotProcesses.ContainsKey(data.Key);
        }

        public void RemoveDotProcess(DotData key)
        {
            _dotProcesses.Remove(key.Key);
        }
    }

    public class DotProcess
    {
        public readonly DotData Data;
        public readonly Coroutine Coroutine;

        public DotProcess(DotData data, Coroutine coroutine)
        {
            Data = data;
            Coroutine = coroutine;
        }
    }

    public class DotData
    {
        public readonly long EffectInstanceId;
        public readonly float Value;
        public readonly DotDamageType DotDamageType;
        public readonly bool IgnoreArmor;
        public readonly float Interval;
        public readonly DamageContext Context;

        public long Key => EffectInstanceId;

        public DotData(DotEffect dotEffect, float duration, DamageContext context)
        {
            if (dotEffect == null)
                throw new ArgumentNullException(nameof(dotEffect));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (dotEffect.DotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dotEffect),
                    dotEffect.DotCount,
                    "DOT count must be greater than zero.");
            }
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    duration,
                    "DOT duration must be a finite number greater than zero.");
            }

            EffectInstanceId = dotEffect.RuntimeEffectInstanceId;
            if (EffectInstanceId <= 0)
            {
                throw new InvalidOperationException(
                    $"DOT effect '{dotEffect.name}' is not a RuntimeExecution clone.");
            }
            Value = dotEffect.Value;
            DotDamageType = dotEffect.ApplyType;
            IgnoreArmor = dotEffect.IgnoreArmor;
            Interval = duration / dotEffect.DotCount;
            Context = context;
        }
    }

    public class DotEffectManager : MonoBehaviour, IDotService
    {
        private readonly Dictionary<ICombatant, DotProcessBundle> _dots = new();
        private IDamageApplier _damageApplier;
        private DamageCalculator _damageCalculator;
        private IFatalStopService _fatalStop;

        public void Init(
            IDamageApplier damageApplier,
            DamageCalculator damageCalculator,
            IFatalStopService fatalStop)
        {
            _damageApplier = damageApplier
                ?? throw new ArgumentNullException(nameof(damageApplier));
            _damageCalculator = damageCalculator
                ?? throw new ArgumentNullException(nameof(damageCalculator));
            _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));
        }

        public IRuntimeEffectHandle ApplyDotEffect(DotData dotData)
        {
            if (dotData == null)
                throw new ArgumentNullException(nameof(dotData));
            if (float.IsNaN(dotData.Interval)
                || float.IsInfinity(dotData.Interval)
                || dotData.Interval <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dotData),
                    dotData.Interval,
                    "DOT interval must be a finite number greater than zero.");
            }

            ICombatant target = dotData.Context.Target
                ?? throw new InvalidOperationException("DOT target is null.");

            if (target is not IDamageable)
            {
                throw new InvalidOperationException(
                    $"DOT effect requires an {nameof(IDamageable)} target. " +
                    $"Received: {target.GetType().Name}.");
            }

            if (!target.IsActive)
                return RuntimeEffectHandle.Empty;

            if (!_dots.TryGetValue(target, out DotProcessBundle bundle))
            {
                bundle = new DotProcessBundle();
                _dots.Add(target, bundle);
                target.OnActiveOff += ReleaseCombatAllDot;
            }

            if (bundle.Dots >= 5 || bundle.Contains(dotData))
                return RuntimeEffectHandle.Empty;

            Coroutine dotCoroutine = StartCoroutine(CoDot(dotData));
            bundle.AddDotProcess(dotData, new DotProcess(dotData, dotCoroutine));

            return new RuntimeEffectHandle(() => ReleaseDotEffect(target, dotData));
        }

        private IEnumerator CoDot(DotData dotData)
        {
            float timer = 0f;

            while (true)
            {
                yield return null;
                timer += Time.deltaTime;

                while (timer >= dotData.Interval)
                {
                    try
                    {
                        ApplyDot(dotData);
                    }
                    catch (Exception exception)
                    {
                        RemoveDotTracking(dotData.Context.Target, dotData);
                        _fatalStop.FatalStop(exception, "DOT effect execution failed.");
                        throw;
                    }

                    timer -= dotData.Interval;
                }
            }
        }

        private int GetDotDamage(
            DotDamageType type,
            ICombatant target,
            float value,
            int damage)
        {
            switch (type)
            {
                case DotDamageType.Fixed:
                    return (int)value;

                case DotDamageType.DamageRatio:
                    return Mathf.RoundToInt(damage * value);

                case DotDamageType.CurrentHPRatio:
                    return target is IDamageable currentHpTarget
                        ? (int)(currentHpTarget.CurrentHP * value)
                        : 0;

                case DotDamageType.MaxHPRatio:
                    return target is IDamageable maxHpTarget
                        ? (int)(maxHpTarget.MaxHP * value)
                        : 0;

                default:
                    return 0;
            }
        }

        private void ApplyDot(DotData dotData)
        {
            if (dotData.Context.Target is not IDamageable damageable)
                return;

            int calculatedDamage = GetDotDamage(
                dotData.DotDamageType,
                damageable,
                dotData.Value,
                dotData.Context.AttackPayload.AttackDamage);
            int appliedDamage = _damageCalculator.CalculateDotDamage(
                damageable,
                calculatedDamage,
                dotData.Context.AttackPayload,
                dotData.IgnoreArmor);

            _damageApplier.TryApply(damageable, appliedDamage);
        }

        private void ReleaseCombatAllDot(ICombatant target)
        {
            if (!_dots.TryGetValue(target, out DotProcessBundle bundle))
                return;

            foreach (DotProcess process in bundle.AllProcesses)
                ReleaseDotEffect(target, process.Data);
        }

        private void ReleaseDotEffect(ICombatant target, DotData dotData)
        {
            if (!_dots.TryGetValue(target, out DotProcessBundle bundle)
                || !bundle.TryGetProcess(dotData, out DotProcess process))
            {
                return;
            }

            StopCoroutine(process.Coroutine);
            bundle.RemoveDotProcess(dotData);

            if (bundle.Dots != 0)
                return;

            _dots.Remove(target);
            target.OnActiveOff -= ReleaseCombatAllDot;
        }

        private void RemoveDotTracking(ICombatant target, DotData dotData)
        {
            if (target == null
                || !_dots.TryGetValue(target, out DotProcessBundle bundle)
                || !bundle.Contains(dotData))
            {
                return;
            }

            bundle.RemoveDotProcess(dotData);
            if (bundle.Dots != 0)
                return;

            _dots.Remove(target);
            target.OnActiveOff -= ReleaseCombatAllDot;
        }

        private void OnDisable()
        {
            var targets = new List<ICombatant>(_dots.Keys);

            foreach (ICombatant target in targets)
                ReleaseCombatAllDot(target);
        }
    }
}
