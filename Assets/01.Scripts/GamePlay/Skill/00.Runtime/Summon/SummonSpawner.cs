using Combat;
using Enemies;
using Skill;
using Skill.Data;
using Skill.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Infrastructure;

namespace Skill.Summon
{
    public class SummonSpawner : MonoBehaviour, ISummonProvider
    {
        [SerializeField] private SummonItem _item;

        private ObjectPool<SummonItem> _summonItemPool = new ObjectPool<SummonItem>();
        private Dictionary<SummonMoveType, Stack<ISummonMoveStrategy>> _summonMoveStrategies = new Dictionary<SummonMoveType, Stack<ISummonMoveStrategy>>();

        ISpriteRepository _spriteRepository;
        ICombatService _combatService;

        public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
        {
            _spriteRepository = spriteRepository;
            _combatService = combatService;

            _summonItemPool.OnCreateEvent += SummonItemInit;
            _summonItemPool.Init(this.transform, _item, 5);
        }

        private void SummonItemInit(SummonItem item)
        {
            SummonExecuteTimer timer = new SummonExecuteTimer();
            item.Initialize(timer);
        }

        public Vector3 GetSpawnPosition(Vector3 pivot, ESpawnPosition PositionType)
        {
            switch (PositionType)
            {
                case ESpawnPosition.TargetPivot:
                    return pivot;
                case ESpawnPosition.TargetUpper:
                    return pivot + Vector3.up * 0.5f;
                case ESpawnPosition.TargetLower:
                    return pivot + Vector3.down * 0.5f;
                case ESpawnPosition.TargetRight:
                    return pivot + Vector3.right * 0.5f;
                case ESpawnPosition.TargetLeft:
                    return pivot + Vector3.left * 0.5f;
                case ESpawnPosition.ScreenCenter:
                default:
                    return Vector3.zero;
            }
        }
        private ISummonMoveStrategy GetMoveStretagy(SummonMove move, Transform owner, ICombatant target)
        {
            ISummonMoveStrategy moveStrategy = default;

            if (_summonMoveStrategies.TryGetValue(move.Move, out var value))
            {
                if (value.Count > 0)
                    moveStrategy = value.Pop();
            }
            else
            {
                switch (move.Move)
                {
                    case SummonMoveType.None:
                        moveStrategy = new NoneMoveStrategy();
                        break;
                    case SummonMoveType.ToTarget:
                        moveStrategy = new ToTargetMoveStrategy();
                        break;
                    case SummonMoveType.Attach:
                        moveStrategy = new AttachMoveStrategy();
                        break;
                }
            }

            moveStrategy.Init(owner, target, move.Speed);
            return moveStrategy;
        }
        public void SpawnSummon(SummonItemData dataSO, SkillPayload skillPayload)
        {
            Vector3 spawnPosition = skillPayload.Target.Position;
            var summonObj = _summonItemPool.GetItem(spawnPosition);

            Sprite sprite = _spriteRepository.GetSprite(dataSO.Sprite);
            ISummonMoveStrategy move = new NoneMoveStrategy();

            if (dataSO is AttachSummonData)
            {
                move = new AttachMoveStrategy();
            }
            else if (dataSO is MoveableSummonData moveableSummonData)
            {
                move = new ToTargetMoveStrategy();
                move.Init(summonObj.transform, skillPayload.Target, moveableSummonData.Speed);
            }

            if (!(dataSO is MoveableSummonData))
            {
                move.Init(summonObj.transform, skillPayload.Target, 0f);
            }

            summonObj.Init(skillPayload, move, dataSO, sprite, _combatService);
        }
    }
}



