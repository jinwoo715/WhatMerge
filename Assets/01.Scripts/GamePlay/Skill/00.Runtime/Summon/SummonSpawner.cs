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
    public interface ISummonExecution
    {
        void Execute(DamageContext damageContext);
    }

    public class AttachSummonExecute : ISummonExecution
    {
        public void Execute(DamageContext damageContext)
        {
            var target = damageContext.Target;
        }
    }

    public class SummonSpawner : MonoBehaviour, ISummonProvider
    {
        [SerializeField] private SummonItem _originSummonItem;

        private ObjectPool<SummonItem> _summonItemPool = new ObjectPool<SummonItem>();

        private ISpriteRepository _spriteRepository;
        private ICombatService _combatService;

        public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
        {
            _spriteRepository = spriteRepository;
            _combatService = combatService;

            _summonItemPool.OnCreateEvent += (item) => { item.OnExecute += ProcessSummonExecuteEffect; };
            _summonItemPool.OnCreateEvent += (item) => { item.OnReturn += ReturnToPool; }; 
            _summonItemPool.Init(this.transform, _originSummonItem, 5);
        }
        public void SpawnSummon(SummonItemData dataSO, DamageContext damageContext)
        {
            if (damageContext == null || damageContext.Target == null)
                return;

            Sprite sprite = _spriteRepository.GetSprite(dataSO.Sprite);

            Vector3 spawnPosition = damageContext.Target.Position;

            SummonItem summonObj = _summonItemPool.GetItem(spawnPosition);

            ISummonMoveStrategy strategy = SummonMoveFactory.GetMoveStrategy(dataSO, summonObj.transform, damageContext.Target);
            summonObj.Init(damageContext, strategy, dataSO, sprite);
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

    public class SummonMoveFactory
    {
        public static ISummonMoveStrategy GetMoveStrategy(SummonItemData summonItemData, Transform owner, ICombatant target)
        {
            return summonItemData switch
            {
                //AttachSummonData => new AttachMoveStrategy(owner, target),
                //MoveableSummonData => new ToTargetMoveStrategy(owner, target, summonItemData),
                _=> new NoneMoveStrategy(),
            };
        }
    }
}



