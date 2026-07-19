using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Summon
{
    public class SummonItem : MonoBehaviour, IPooledItem<SummonItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private ISummonMoveStrategy _move;
        private ISummonExecutionStrategy _execution;

        public bool IsActive { get; private set; }
        public event Action<SummonItem> OnReturn;

        private float _currentTimer;
        private float _duration;

        internal void Init(ISummonMoveStrategy move, ISummonExecutionStrategy execution, float duration)
        {
            _currentTimer = 0;
            _execution = execution;
            _duration = duration;
            _move = move;
            SetMove(_move);
        }

        private void Update()
        {
            if (!IsActive)
                return;

            float deltaTime = Time.deltaTime;
            _currentTimer += deltaTime;

            _execution.OnTick(deltaTime);
            _move?.Tick(deltaTime);

            if (!IsActive)
                return;

            if (_currentTimer >= _duration)
                ExecuteAndExpire();
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy")) 
            {
                _execution.OnEnter(other.GetComponent<IDamageable>());
            }
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                _execution.OnExit(other.GetComponent<IDamageable>());
            }
        }
        public void OnDespawn()
        {
            IsActive = false;
            UnbindMoveEvent();
            _move = null;
        }
        public void OnSpawn()
        {
            IsActive = true;
        }
        private void SetMove(ISummonMoveStrategy move)
        {
            
            UnbindMoveEvent();
            _move.OnTargetLost += OnTargetLost;
        }
        private void UnbindMoveEvent()
        {
            if (_move != null)
                _move.OnTargetLost -= OnTargetLost;
        }

        private void OnTargetLost(TargetLostEventType eventType)
        {
            switch (eventType)
            {
                case TargetLostEventType.Disappear:
                    Expire();
                    break;

                case TargetLostEventType.OnExecute:
                    ExecuteAndExpire();
                    break;
            }
        }
        
        private void ExecuteAndExpire()
        {
            _execution.OnExpire();
            Expire();
        }
        private void Expire()
        {
            _execution.Dispose();
            OnReturn?.Invoke(this);
        }
    }
}
