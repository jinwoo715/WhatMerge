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
        private ISpriteRepository _spriteRepository;
        private IFatalStopService _fatalStop;

        public void Init(
            ISpriteRepository spriteRepository,
            ICombatService combatService,
            IFatalStopService fatalStop)
        {
            _combatService = combatService;

            _spriteRepository = spriteRepository;
            _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));
            _summonItemPool.OnCreateEvent += (item) => { item.OnReturn += ReturnToPool; }; 
            _summonItemPool.Init(this.transform, _originSummonItem, 5);
        }

        public void SpawnSummon(SummonSpawnEffect dataSO, DamageContext damageContext)
        {
            if (dataSO == null)
                throw new ArgumentNullException(nameof(dataSO));
            if (damageContext == null)
                throw new ArgumentNullException(nameof(damageContext));

            ICombatant target = damageContext.Target;
            if (SummonMoveFactory.RequiresTarget(dataSO.Move))
            {
                if (target == null)
                {
                    throw new InvalidOperationException(
                        $"Summon move '{dataSO.Move.GetType().Name}' requires a target.");
                }

                if (!target.IsActive)
                    return;
            }

            ValidatePositiveFinite(dataSO.DurationTime, nameof(dataSO.DurationTime), dataSO.name);

            Vector3 spawnPosition = GetSpawnPosition(damageContext.ImpactPosition, dataSO.SpawnPosition);
            SummonItem summonObj = _summonItemPool.GetItem(spawnPosition);
            ISummonMoveStrategy move = null;
            ISummonExecutionStrategy execution = null;
            IDisposable effectLifetimeLease = null;

            try
            {
                move = SummonMoveFactory.GetMoveStrategy(
                    dataSO.Move,
                    summonObj.transform,
                    target,
                    dataSO.DurationTime);
                execution = SummonExecutionFactory.GetExecutionStrategy(
                    dataSO.Execution,
                    damageContext.WithoutTarget(),
                    _combatService,
                    dataSO.DurationTime);
                execution.OnExecuteEffect += ProcessSummonExecuteEffect;
                effectLifetimeLease = damageContext.RetainEffectLifetime();

                Sprite summonSprite = _spriteRepository.GetSprite(dataSO.SummonSpriteName);
                summonObj.Init(
                    move,
                    execution,
                    dataSO.DurationTime,
                    effectLifetimeLease,
                    summonSprite,
                    _fatalStop);

                move = null;
                execution = null;
                effectLifetimeLease = null;
            }
            catch (Exception exception)
            {
                TryDispose(move);

                if (execution != null)
                    execution.OnExecuteEffect -= ProcessSummonExecuteEffect;
                TryDispose(execution);
                TryDispose(effectLifetimeLease);
                TryReturnSummon(summonObj);

                _fatalStop.FatalStop(exception, $"Summon spawn failed. Effect:{dataSO.name}.");
                throw;
            }
        }

        private Vector3 GetSpawnPosition(Vector3 pivot, ESpawnPosition spawnType)
        {
            switch (spawnType)
            {
                case ESpawnPosition.Pivot:
                    break;
                case ESpawnPosition.Upper:
                    pivot += Vector3.up;
                    break;
                case ESpawnPosition.Lower:
                    pivot += Vector3.down;
                    break;
                case ESpawnPosition.Right:
                    pivot += Vector3.right;
                    break;
                case ESpawnPosition.Left:
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

        private void TryReturnSummon(SummonItem item)
        {
            try
            {
                if (item != null && item.IsActive)
                    _summonItemPool.ReturnItem(item);
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }
        }

        private static void TryDispose(IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }
        }
    }
    public class SummonExecutionFactory
    {
        public static ISummonExecutionStrategy GetExecutionStrategy(SummonExecutionData execution, DamageContext damageContext, ICombatService combatService, float duration)
        {
            return execution switch
            {
                OnEnterExecutionSummon => new OnEnterExecution(damageContext),
                OnTickExecutionSummon tickExecution => new OnTickExecution(damageContext, tickExecution.TickTime),
                SummonOnStayExecution => new OnStayExecution(damageContext, combatService),
                OnExpireExecutionSummon => new OnExpireExecution(damageContext),
                OnTimeOnceExecutionSummon onceExecution => new OnTimeOncewExecution(damageContext, duration, onceExecution.ExecutionTiming),
                _ => throw new System.InvalidOperationException(
                    $"Unsupported summon execution type: {execution?.GetType().Name ?? "null"}.")
            };
        }
    }

    public class SummonMoveFactory
    {
        public static bool RequiresTarget(SummonMove summonMove)
        {
            return summonMove is SummonAttachMove or SummonApproachMove;
        }

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



