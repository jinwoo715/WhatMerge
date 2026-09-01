using System;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;
using WhatMerge.Heros;
using WhatMerge.Projectiles.Data;

namespace WhatMerge.Projectiles
{
    public class ProjectileItem : MonoBehaviour, IPooledItem<ProjectileItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private float _currentTime;

        private IProjectile _stretagy;
        
        private ICombatService _combatService;

        private ProjectileDataBase _soData;

        private DamageContext _damageContext;
        private IDisposable _effectLifetimeLease;
        private IFatalStopService _fatalStop;
        private bool _isReturning;

        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        public void Init(DamageContext damageContext, IProjectile stretagy, ProjectileDataBase soData, Sprite sprite,
            ICombatService combatService,
            IDisposable effectLifetimeLease,
            IFatalStopService fatalStop)
        {
            if (_effectLifetimeLease != null)
                throw new InvalidOperationException("Projectile effect lifetime is already assigned.");

            _combatService = combatService;
            _damageContext = damageContext;
            _stretagy = stretagy;
            _renderer.sprite = sprite;
            _soData = soData;
            _effectLifetimeLease = effectLifetimeLease;
            _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));
            _currentTime = 0;
            _stretagy.OnExecute += Execute;
            _stretagy.OnExpired += Expired;
        }

        private void Update()
        {
            if (!IsActive || _stretagy == null)
                return;

            try
            {
                _currentTime += Time.deltaTime;
                _stretagy.Tick(Time.deltaTime);

                if (!IsActive || _isReturning || _soData == null)
                    return;

                if (_currentTime >= _soData.LifeTime)
                    Expired();
            }
            catch (Exception exception)
            {
                HandleFatal(exception, "Projectile update failed.");
                throw;
            }
        }

        private void Expired()
        {
            if (!IsActive || _isReturning)
                return;

            _isReturning = true;
            OnReturn?.Invoke(this);
        }

        private void Execute(ProjectileImpact impact)
        {
            if (!IsActive || _isReturning)
                return;

            DamageContext context = impact.Target != null ? _damageContext.WithTarget(impact.Target) : _damageContext.WithImpactPosition(impact.Position);

            _combatService.RegisterAttack(context);
        }

        public void OnDespawn()
        {
            IsActive = false;
            _currentTime = 0;
            CleanupRuntime();
        }

        private void CleanupRuntime()
        {
            Exception firstException = null;
            IProjectile strategy = _stretagy;
            _stretagy = null;

            if (strategy != null)
            {
                strategy.OnExecute -= Execute;
                strategy.OnExpired -= Expired;

                try
                {
                    strategy.Dispose();
                }
                catch (Exception exception)
                {
                    firstException = exception;
                }
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
            _damageContext = null;
            _soData = null;
            _fatalStop = null;

            if (firstException != null)
                throw firstException;
        }
        public void OnSpawn()
        {
            IsActive = true;
            _isReturning = false;
            transform.rotation = Quaternion.identity;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsActive || _isReturning || _stretagy == null)
                return;

            try
            {
                if (collision.TryGetComponent(out ICombatant target)
                    && IsCompatibleTarget(target)
                    && target.IsActive)
                {
                    _stretagy.HitTarget(target);
                }
            }
            catch (Exception exception)
            {
                HandleFatal(exception, "Projectile collision failed.");
                throw;
            }
        }

        private bool IsCompatibleTarget(ICombatant target)
        {
            return _damageContext?.TargetKind switch
            {
                CombatantTargetKind.Hero => target is Hero,
                CombatantTargetKind.Enemy => target is Enemy,
                CombatantTargetKind.Other => false,
                _ => false,
            };
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
            if (_stretagy == null && _effectLifetimeLease == null)
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
