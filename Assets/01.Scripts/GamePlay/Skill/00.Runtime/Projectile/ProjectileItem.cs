using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Projectile
{
    public class ProjectileItem : MonoBehaviour, IPooledItem<ProjectileItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private float _currentTime;

        private IProjectileMoveStrategy _moveStretagy;
        
        private ICombatService _combatService;

        private Data.ProjectileDataBase _soData;

        private DamageContext _damageContext;

        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        public void Init(DamageContext damageContext, IProjectileMoveStrategy moveStretagy, Data.ProjectileDataBase soData, Sprite sprite, ICombatService combatService)
        {
            _combatService = combatService;
            _damageContext = damageContext;
            _moveStretagy = moveStretagy;
            _renderer.sprite = sprite;
            _soData = soData;
        }

        private void Update()
        {
            if (IsActive == false) return;

            //시간 초과
            if (_currentTime >= _soData.LifeTime)
            {
                CheckTrigger(EProjectileEffectTrigger.OnTimeOut, null);
                return;
            }

            if (IsActive == false) return;
            
            _currentTime += Time.deltaTime;
            _moveStretagy.Tick(Time.deltaTime);
        }

        private void CheckTrigger(EProjectileEffectTrigger trigger, IDamageable target)
        {
            _combatService.RegisterAttack(_damageContext.WithTarget(target));

            OnReturn?.Invoke(this);
        }

        public void OnDespawn()
        {
            IsActive = false;
            _currentTime = 0;
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
                    if (!IsActive) return;

                    CheckTrigger(EProjectileEffectTrigger.OnHit, target);
                }
            }
        }
    }
}