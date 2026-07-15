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
        private SummonItemData _summonData;
        private ISummonMoveStrategy _move;
        private DamageContext _damageContext;

        public bool IsActive { get; private set; }
        public event Action<SummonItem> OnReturn;
        public event Action<DamageContext> OnExecute;

        private float _currentTimer;

        private void ExecuteAndExpire()
        {
            OnExecute?.Invoke(_damageContext);
            Expire();
        }

        private void Expire()
        {
            OnReturn?.Invoke(this);
        }

        private void OnTargetLost()
        {
            switch (GetTargetLostEventType())
            {
                case TargetLostEventType.Disappear:
                    Expire();
                    break;

                case TargetLostEventType.OnExecute:
                    ExecuteAndExpire();
                    break;
            }
        }

        private TargetLostEventType GetTargetLostEventType()
        {
            //if (_summonData is AttachSummonData attachSummonData)
            //    return attachSummonData.TargetLostEventType;

            //if (_summonData is MoveableSummonData moveableSummonData)
            //    return moveableSummonData.TargetLostEventType;

            return TargetLostEventType.Disappear;
        }

        internal void Init(DamageContext damageContext, ISummonMoveStrategy move, SummonItemData summonData, Sprite sprite)
        {
            _move = move;
            _summonData = summonData;
            _renderer.sprite = sprite;
            _damageContext = damageContext;

            _move.OnTargetLost += OnTargetLost;

            _currentTimer = 0;
        }
        private void Update()
        {
            if (!IsActive)
                return;

            _currentTimer += Time.deltaTime;

            _move.Tick(Time.deltaTime);

            if (!IsActive)
                return;

            if (_currentTimer >= _summonData.Duration)
            {
                ExecuteAndExpire();
            }
        }
        public void OnDespawn()
        {
            IsActive = false;
            _move.OnTargetLost -= OnTargetLost;
        }
        public void OnSpawn()
        {
            IsActive = true;
        }
    }
}

