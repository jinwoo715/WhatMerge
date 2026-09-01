using System;
using System.Collections.Generic;
using WhatMerge.Combat;
using WhatMerge.Combat.Effects;
using UnityEngine;

namespace WhatMerge.Summons
{
    public interface ISummonExecutionStrategy : IDisposable
    {
        event Action<DamageContext> OnExecuteEffect;
        void OnEnter(ICombatant combatant);
        void OnExpire();
        void OnTick(float tick);
        void OnExit(ICombatant combatant);
        void SetSourcePosition(Vector3 position);
    }

    public abstract class SummonExecution : ISummonExecutionStrategy
    {
        protected DamageContext _damageContext;

        public event Action<DamageContext> OnExecuteEffect;
        public virtual void OnEnter(ICombatant combatant) { }
        public virtual void OnExit(ICombatant combatant) { }
        public virtual void OnExpire() { }
        public virtual void OnTick(float tick) { }
        public void SetSourcePosition(Vector3 position)
        {
            if (_damageContext != null)
                _damageContext = _damageContext.WithSourcePosition(position);
        }
        public void ExecuteEffect(DamageContext damageContext)
        {
            OnExecuteEffect?.Invoke(damageContext);
        }

        public virtual void Dispose() { OnExecuteEffect = null; }
    }

    public class OnTimeOncewExecution : SummonExecution
    {
        private bool _hasExecuted = false;
        private float _executionTiming;
        private float _currentTime;
        public OnTimeOncewExecution(DamageContext damageContext, float duration, float executionTiming)
        {
            _hasExecuted = false;
            _damageContext = damageContext;
            _executionTiming = duration * executionTiming;
        }

        public override void OnTick(float tick)
        {
            if (_hasExecuted)
                return;

            _currentTime += tick;

            if(_currentTime >= _executionTiming)
            {
                ExecuteEffect(_damageContext);
                _hasExecuted = true;
            }
        }

    }
    public class OnExpireExecution : SummonExecution
    {
        public OnExpireExecution(DamageContext damageContext)
        {
            _damageContext = damageContext;
        }

        public override void OnExpire()
        {
            ExecuteEffect(_damageContext);
        }
    }

    public class OnStayExecution : SummonExecution, IDisposable
    {
        private readonly Dictionary<ICombatant, IRuntimeEffectHandle> _handles = new();
        private readonly ICombatService _combatService;
        private bool _isDisposed;

        public OnStayExecution(DamageContext damageContext, ICombatService combatService)
        {
            _damageContext = damageContext ?? throw new ArgumentNullException(nameof(damageContext));
            _combatService = combatService ?? throw new ArgumentNullException(nameof(combatService));
        }

        public override void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            base.Dispose();
            var targets = new List<ICombatant>(_handles.Keys);
            Exception firstException = null;

            foreach (ICombatant target in targets)
            {
                try
                {
                    ReleaseTarget(target);
                }
                catch (Exception exception)
                {
                    firstException ??= exception;
                }
            }

            if (firstException != null)
                throw firstException;
        }

        public override void OnEnter(ICombatant combatant)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(OnStayExecution));
            if (combatant == null)
                throw new ArgumentNullException(nameof(combatant));
            if (!combatant.IsActive || _handles.ContainsKey(combatant))
                return;

            DamageContext context = _damageContext.WithTarget(combatant);
            IRuntimeEffectHandle handle = _combatService.ApplyPersistentEffects(context);

            if (!combatant.IsActive)
            {
                handle.Dispose();
                return;
            }

            _handles.Add(combatant, handle);
            combatant.OnActiveOff += ReleaseTarget;
        }

        public override void OnExit(ICombatant combatant)
        {
            ReleaseTarget(combatant);
        }

        public override void OnExpire()
        {
            Dispose();
        }

        private void ReleaseTarget(ICombatant combatant)
        {
            if (combatant == null
                || !_handles.TryGetValue(combatant, out IRuntimeEffectHandle handle))
            {
                return;
            }

            _handles.Remove(combatant);
            combatant.OnActiveOff -= ReleaseTarget;
            handle.Dispose();
        }
    }

    public class OnEnterExecution : SummonExecution
    {
        public OnEnterExecution(DamageContext damageContext)
        {
            _damageContext = damageContext;
        }

        public override void OnEnter(ICombatant combatant)
        {
            ExecuteEffect(_damageContext.WithTarget(combatant));
        }
    }
    public class OnTickExecution : SummonExecution
    {
        private float _tick;
        private float _current;
        public OnTickExecution(DamageContext damageContext, float tickTime)
        {
            if (float.IsNaN(tickTime)
                || float.IsInfinity(tickTime)
                || tickTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tickTime),
                    tickTime,
                    "Summon tick time must be a finite number greater than zero.");
            }

            _tick = tickTime;
            _current = 0;
            _damageContext = damageContext;
        }

        public override void OnTick(float tick)
        {
            _current += tick;

            if(_current >= _tick)
            {
                ExecuteEffect(_damageContext);
                _current = 0;
            }
        }
    }
}
