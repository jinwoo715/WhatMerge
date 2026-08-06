using Skill;
using Skill.Data;
using System;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Infrastructure;
using WhatMerge.Summons.Data;

namespace WhatMerge.Summons
{
    public class SummonSpawner : MonoBehaviour, ISummonProvider
    {
        [SerializeField] private SummonItem _originSummonItem;

        private ObjectPool<SummonItem> _summonItemPool = new ObjectPool<SummonItem>();

        private ICombatService _combatService;

        public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
        {
            _combatService = combatService;

            _summonItemPool.OnCreateEvent += (item) => { item.OnReturn += ReturnToPool; }; 
            _summonItemPool.Init(this.transform, _originSummonItem, 5);
        }

        public void SpawnSummon(SummonSpawnEffect dataSO, DamageContext damageContext)
        {
            if (dataSO == null)
                throw new ArgumentNullException(nameof(dataSO));
            if (damageContext == null)
                throw new ArgumentNullException(nameof(damageContext));
            if (damageContext.Target == null)
                throw new InvalidOperationException("Summon effect requires a target.");
            if (!damageContext.Target.IsActive)
                return;

            ValidatePositiveFinite(dataSO.DurationTime, nameof(dataSO.DurationTime), dataSO.name);

            Vector3 spawnPosition = GetSpawnPosition(damageContext.Target.Position, dataSO.SpawnPosition);
            SummonItem summonObj = _summonItemPool.GetItem(spawnPosition);

            ISummonMoveStrategy move = SummonMoveFactory.GetMoveStrategy(dataSO.Move, summonObj.transform, damageContext.Target, dataSO.DurationTime);
            ISummonExecutionStrategy execution = SummonExecutionFactory.GetExecutionStrategy(
                dataSO.Execution,
                damageContext,
                _combatService);
            execution.OnExecuteEffect += ProcessSummonExecuteEffect;

            IDisposable effectLifetimeLease = damageContext.RetainEffectLifetime();

            try
            {
                summonObj.Init(move, execution, dataSO.DurationTime, effectLifetimeLease);
                effectLifetimeLease = null;
            }
            finally
            {
                effectLifetimeLease?.Dispose();
            }
        }

        private Vector3 GetSpawnPosition(Vector3 pivot, ESpawnPosition spawnType)
        {
            switch (spawnType)
            {
                case ESpawnPosition.TargetPivot:
                    break;
                case ESpawnPosition.TargetUpper:
                    pivot += Vector3.up;
                    break;
                case ESpawnPosition.TargetLower:
                    pivot += Vector3.down;
                    break;
                case ESpawnPosition.TargetRight:
                    pivot += Vector3.right;
                    break;
                case ESpawnPosition.TargetLeft:
                    pivot += Vector3.left;
                    break;
            }

            return pivot;
        }
        private static void ValidatePositiveFinite(float value, string fieldName, string dataName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName,
                    value,
                    $"Summon effect '{dataName}' {fieldName} must be a finite number greater than zero.");
            }
        }
        private void ReturnToPool(SummonItem item)
        {
            _summonItemPool.ReturnItem(item);
        }
        private void ProcessSummonExecuteEffect(DamageContext damageContext)
        {
            _combatService.RegisterAttack(damageContext);
        }
    }
    public class SummonExecutionFactory
    {
        public static ISummonExecutionStrategy GetExecutionStrategy(
            SummonExecutionData execution,
            DamageContext damageContext,
            ICombatService combatService)
        {
            return execution switch
            {
                OnEnterExecutionSummon => new OnEnterExecution(damageContext),
                OnTickExecutionSummon tickExecution => new OnTickExecution(damageContext, tickExecution.TickTime),
                SummonOnStayExecution => new OnStayExecution(damageContext, combatService),
                OnExpireExecutionSummon => new OnExpireExecution(damageContext),
                _ => throw new System.InvalidOperationException(
                    $"Unsupported summon execution type: {execution?.GetType().Name ?? "null"}.")
            };
        }
    }

    public class SummonMoveFactory
    {
        public static ISummonMoveStrategy GetMoveStrategy(SummonMove summonMove, Transform owner, ICombatant target, float duration)
        {
            var eventType = TargetLostEventType.Disappear;
            if(summonMove is SummonMoveable moveable)
            {
                eventType = moveable.LostTargetEvent;
            }

            return summonMove switch
            {
                SummonAttachMove => new AttachMoveStrategy(owner, target, eventType),
                SummonApproachMove => new ApproachMoveStrategy(owner, target, duration, eventType),
                SummonNoneMove => new NoneMoveStrategy(),
                _ => throw new System.InvalidOperationException(
                    $"Unsupported summon move type: {summonMove?.GetType().Name ?? "null"}."),
            };
        }

    }
}



