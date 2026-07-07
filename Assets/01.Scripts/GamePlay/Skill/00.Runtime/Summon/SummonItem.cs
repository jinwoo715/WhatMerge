using Combat;
using Skill.Data;
using Skill.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Summon  
{
    public class SummonItem : MonoBehaviour, IPooledItem<SummonItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private SummonExecuteTimer _timer;
        private ICombatService _combatService;

        private SummonItemData _summonData;
        private ISummonMoveStrategy _move;
        private SkillPayload _payload;

        public bool IsActive { get; private set; }
        public event Action<SummonItem> OnReturn;
        private Action OnTimeOut;

        public void Initialize(SummonExecuteTimer timer)
        {
            _timer = timer;
            _timer.OnExecute += Execute;

            OnTimeOut += () => OnReturn?.Invoke(this);

            _timer.OnTimeOut += TimeOut;
        }
        public void TimeOut()
        {
            OnReturn?.Invoke(this);
            Debug.Log("TimeOut");
        }

        internal void Init(SkillPayload payload, ISummonMoveStrategy move, SummonItemData summonData, Sprite sprite, ICombatService combatService)
        {
            _combatService = combatService;

            _move = move;
            _summonData = summonData;
            _renderer.sprite = sprite;
            _payload = payload;

            _move.OnLooseTarget += TimeOut;

            _timer.Init(_summonData.Duration, new SummonApplyTiming { ApplyType = SummonApplyType.Once });
        }

        private void Update()
        {
            _move.Tick();
            _timer.Tick();
        }

        private void Execute()
        {
            DamageContext context = new DamageContext(_payload.payLoad, _payload.Target as IDamageable, _payload.Attacker);
            context.skillEffects = _payload.effects;
            _combatService.RegisterAttack(context);
        }

        public void OnDespawn()
        {
            IsActive = false;
            _move.OnLooseTarget -= TimeOut;
        }
        public void OnSpawn()
        {
            IsActive = true;
        }
    }
}

