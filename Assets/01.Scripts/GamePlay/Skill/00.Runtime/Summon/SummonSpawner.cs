using Skill;
using Skill.Data;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Infrastructure;

namespace Skill.Summon
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
            if (dataSO == null || damageContext == null || damageContext.Target == null)
                return;

            Vector3 spawnPosition = GetSpawnPosition(damageContext.Target.Position, dataSO.SpawnPosition);
            SummonItem summonObj = _summonItemPool.GetItem(spawnPosition);

            ISummonMoveStrategy move = SummonMoveFactory.GetMoveStrategy(dataSO.Move, summonObj.transform, damageContext.Target, dataSO.DurationTime);
            ISummonExecutionStrategy execution = SummonExecutionFactory.GetExecutionStrategy(dataSO.Execution, damageContext);
            execution.OnExecuteEffect += ProcessSummonExecuteEffect;
            summonObj.Init(move, execution, dataSO.DurationTime);
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
        public static ISummonExecutionStrategy GetExecutionStrategy(SummonExecutionData execution, DamageContext damageContext)
        {
            return execution switch
            {
                OnEnterExecutionSummon => new OnEnterExecution(damageContext),
                OnTickExecutionSummon => new OnTickExecution(damageContext, (execution as OnTickExecutionSummon).TickTime),
                SummonOnStayExecution => new OnStayExecution(damageContext),
                OnExpireExecutionSummon => new OnExpireExecution(damageContext),
                _ => new OnExpireExecution(damageContext)
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
                _ => new NoneMoveStrategy(),
            };
        }

    }
}



