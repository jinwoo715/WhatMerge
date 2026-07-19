using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;

namespace Skill
{
    public interface ISummonExecutionStrategy
    {
        event Action<DamageContext> OnExecuteEffect;
        void OnEnter(ICombatant combatant);
        void OnExpire();
        void OnTick(float tick);
        void OnExit(ICombatant combatant);
        public void Dispose();
    }

    public abstract class SummonExecution : ISummonExecutionStrategy
    {
        protected DamageContext _damageContext;

        public event Action<DamageContext> OnExecuteEffect;
        public virtual void OnEnter(ICombatant combatant) { }
        public virtual void OnExit(ICombatant combatant) { }
        public virtual void OnExpire() { }
        public virtual void OnTick(float tick) { }
        public void ExecuteEffect(DamageContext damageContext)
        {
            OnExecuteEffect?.Invoke(damageContext);
        }

        public virtual void Dispose() { }
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

    public class CompositeEffectHandle : IDisposable
    {
        public DamageContext _context;
        public DamageContext _reverseContext;

        public CompositeEffectHandle(DamageContext context, ICombatant combatant)
        {
            _context = new DamageContext(
            context.AttackPayload,
            combatant, context.Attacker, context.SkillUid, context.OwnerSpawnIndex, context.Effects);

            SetRevertEffects(context);
        }

        private void SetRevertEffects(DamageContext origin)
        {
            List<EffectBase> effects = new();

            _reverseContext = new DamageContext(
           origin.AttackPayload,
           origin.Target, origin.Attacker, origin.SkillUid, origin.OwnerSpawnIndex, effects);

            foreach (var effect in origin.Effects)
            {
                if (effect is IRevert)
                {
                    EffectBase revertEffect = UnityEngine.Object.Instantiate(effect);
                    (revertEffect as IRevert).Revert();

                    effects.Add(revertEffect);
                }
            }
        }

        public void Dispose()
        {
            foreach (var obj in _reverseContext.Effects)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        public DamageContext Context => _context;
        public DamageContext ReverseContext => _reverseContext;
    }

    public class OnStayExecution : SummonExecution, IDisposable
    {
        private Dictionary<ICombatant, CompositeEffectHandle> _handles = new();

        public OnStayExecution(DamageContext damageContext)
        {
            _damageContext = damageContext;
        }

        public override void Dispose()
        {
            foreach (var handle in _handles)
            {
                ExecuteEffect(handle.Value.ReverseContext);
                handle.Value.Dispose();
            }
            _handles.Clear();
        }

        public override void OnEnter(ICombatant combatant)
        {
            DamageContext context = new DamageContext(
            _damageContext.AttackPayload,
            combatant, _damageContext.Attacker, _damageContext.SkillUid, _damageContext.OwnerSpawnIndex, _damageContext.Effects);

            CompositeEffectHandle compositeEffectHandle = new CompositeEffectHandle(context, combatant);

            _handles.Add(combatant, compositeEffectHandle);

            ExecuteEffect(compositeEffectHandle.Context);
        }
        public override void OnExit(ICombatant combatant)
        {
            if(_handles.TryGetValue(combatant, out var value))
            {
                ExecuteEffect(value.ReverseContext);
                value.Dispose();
                _handles.Remove(combatant);
            }
        }
        public override void OnExpire()
        {
            foreach (var handle in _handles)
            {
                ExecuteEffect(handle.Value.ReverseContext);
                handle.Value.Dispose();
            }
            _handles.Clear();
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
            DamageContext context = new DamageContext(
            _damageContext.AttackPayload,
            combatant, _damageContext.Attacker, _damageContext.SkillUid, _damageContext.OwnerSpawnIndex, _damageContext.Effects);

            ExecuteEffect(context);
        }
    }
    public class OnTickExecution : SummonExecution
    {
        private float _tick;
        private float _current;
        public OnTickExecution(DamageContext damageContext, float tickTime)
        {
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
