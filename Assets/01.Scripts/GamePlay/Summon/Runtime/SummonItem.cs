using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Summons.Data;

namespace WhatMerge.Summons
{
    public class SummonItem : MonoBehaviour, IPooledItem<SummonItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private ISummonMoveStrategy _move;
        private ISummonExecutionStrategy _execution;
        private IDisposable _effectLifetimeLease;
        private IFatalStopService _fatalStop;
        private bool _isReturning;

        public bool IsActive { get; private set; }
        public event Action<SummonItem> OnReturn;

        private float _currentTimer;
        private float _duration;

        internal void Init(
            ISummonMoveStrategy move,
            ISummonExecutionStrategy execution,
            float duration,
            IDisposable effectLifetimeLease,
            Sprite sprite,
            IFatalStopService fatalStop)
        {
            if (_effectLifetimeLease != null)
                throw new InvalidOperationException("Summon effect lifetime is already assigned.");

            _renderer.sprite = sprite;
            _currentTimer = 0;
            _execution = execution;
            _duration = duration;
            _move = move;
            _effectLifetimeLease = effectLifetimeLease;
            _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));
            SetMove(_move);
        }

        private void Update()
        {
            if (!IsActive)
                return;

            try
            {
                float deltaTime = Time.deltaTime;
                _currentTimer += deltaTime;

                _execution.SetSourcePosition(transform.position);
                _execution.OnTick(deltaTime);
                _move?.Tick(deltaTime);

                if (!IsActive || _isReturning)
                    return;

                if (_currentTimer >= _duration)
                    ExecuteAndExpire();
            }
            catch (Exception exception)
            {
                HandleFatal(exception, "Summon update failed.");
                throw;
            }
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActive || _isReturning || !other.CompareTag("Enemy"))
                return;

            try
            {
                _execution.SetSourcePosition(transform.position);
                _execution.OnEnter(other.GetComponent<IDamageable>());
            }
            catch (Exception exception)
            {
                HandleFatal(exception, "Summon collision enter failed.");
                throw;
            }
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsActive || _isReturning || !other.CompareTag("Enemy"))
                return;

            try
            {
                _execution.SetSourcePosition(transform.position);
                _execution.OnExit(other.GetComponent<IDamageable>());
            }
            catch (Exception exception)
            {
                HandleFatal(exception, "Summon collision exit failed.");
                throw;
            }
        }
        public void OnDespawn()
        {
            IsActive = false;
            _isReturning = true;
            CleanupRuntime();
        }

        private void CleanupRuntime()
        {
            Exception firstException = null;
            UnbindMoveEvent();
            ISummonMoveStrategy move = _move;
            _move = null;

            try
            {
                move?.Dispose();
            }
            catch (Exception exception)
            {
                firstException = exception;
            }

            ISummonExecutionStrategy execution = _execution;
            _execution = null;
            try
            {
                execution?.Dispose();
            }
            catch (Exception exception)
            {
                firstException ??= exception;
                if (!ReferenceEquals(firstException, exception))
                    Debug.LogException(exception);
            }

            try
            {
                _effectLifetimeLease?.Dispose();
            }
            catch (Exception exception)
            {
                firstException ??= exception;
                if (!ReferenceEquals(firstException, exception))
                    Debug.LogException(exception);
            }

            _effectLifetimeLease = null;
            _fatalStop = null;

            if (firstException != null)
                throw firstException;
        }
        public void OnSpawn()
        {
            IsActive = true;
            _isReturning = false;
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
            _execution.SetSourcePosition(transform.position);
            _execution.OnExpire();
            Expire();
        }
        private void Expire()
        {
            if (!IsActive || _isReturning)
                return;

            _isReturning = true;
            OnReturn?.Invoke(this);
        }

        private void HandleFatal(Exception exception, string context)
        {
            IFatalStopService fatalStop = _fatalStop;

            try
            {
                if (IsActive && !_isReturning)
                {
                    _isReturning = true;
                    OnReturn?.Invoke(this);
                }
                else
                {
                    CleanupRuntime();
                }
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }

            fatalStop?.FatalStop(exception, context);
        }

        private void OnDisable()
        {
            if (_move == null && _execution == null && _effectLifetimeLease == null)
                return;

            try
            {
                CleanupRuntime();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
