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
        private bool _isReturning;

        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        public void Init(DamageContext damageContext, IProjectile stretagy, ProjectileDataBase soData, Sprite sprite,
            ICombatService combatService,
            IDisposable effectLifetimeLease)
        {
            if (_effectLifetimeLease != null)
                throw new InvalidOperationException("Projectile effect lifetime is already assigned.");

            _combatService = combatService;
            _damageContext = damageContext;
            _stretagy = stretagy;
            _renderer.sprite = sprite;
            _soData = soData;
            _effectLifetimeLease = effectLifetimeLease;
            _currentTime = 0;
            _stretagy.OnExecute += Execute;
            _stretagy.OnExpired += Expired;
        }

        private void Update()
        {
            if (!IsActive || _stretagy == null)
                return;

            _currentTime += Time.deltaTime;
            _stretagy.Tick(Time.deltaTime);

            if (!IsActive || _isReturning || _soData == null)
                return;

            if (_currentTime >= _soData.LifeTime)
            {
                Expired();
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

            if (_stretagy != null)
            {
                _stretagy.OnExecute -= Execute;
                _stretagy.OnExpired -= Expired;
                _stretagy = null;
            }

            _effectLifetimeLease?.Dispose();
            _effectLifetimeLease = null;
            _damageContext = null;
            _soData = null;
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

            if (collision.TryGetComponent(out ICombatant target)
                && IsCompatibleTarget(target))
            {
                if (target.IsActive)
                    _stretagy.HitTarget(target);
            }
        }

        private bool IsCompatibleTarget(ICombatant target)
        {
            ICombatant intendedTarget = _damageContext?.Target;

            return intendedTarget switch
            {
                Hero => target is Hero,
                Enemy => target is Enemy,
                _ => ReferenceEquals(target, intendedTarget),
            };
        }
    }
}
