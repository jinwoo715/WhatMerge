using System;
using UnityEngine;
using WhatMerge.Combat;
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

        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        public void Init(
            DamageContext damageContext,
            IProjectile stretagy,
            ProjectileDataBase soData,
            Sprite sprite,
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
            if (IsActive == false) return;

            _currentTime += Time.deltaTime;
            _stretagy.Tick(Time.deltaTime);

            if (_currentTime >= _soData.LifeTime)
            {
                Expired();
            }
        }

        private void Expired()
        {
            OnReturn?.Invoke(this);
        }

        private void Execute(ProjectileImpact impact)
        {
            DamageContext context = impact.Target != null
                ? _damageContext.WithTarget(impact.Target)
                : _damageContext.WithImpactPosition(impact.Position);

            _combatService.RegisterAttack(context);
        }

        public void OnDespawn()
        {
            IsActive = false;
            _currentTime = 0;
            _effectLifetimeLease?.Dispose();
            _effectLifetimeLease = null;
        }
        public void OnSpawn()
        {
            IsActive = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy"))
            {
                if(collision.TryGetComponent<IDamageable>(out IDamageable target))
                {
                    if (target.IsActive)
                        _stretagy.HitEnemy(target);
                }
            }
        }
    }
}
