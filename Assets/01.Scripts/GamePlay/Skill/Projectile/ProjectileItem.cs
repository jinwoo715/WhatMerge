using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Projectile
{
    public class ProjectileItem : MonoBehaviour, IPooledItem<ProjectileItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private float _currentTime;

        private EProjectileEffectTrigger _trigger;
        private EProjectileEffectTrigger _destroyTrigger;
        private IProjectileMoveStrategy _moveStretagy;
        
        private SkillExecuter _effectExecuter;

        private Data.ProjectileData _soData;

        private SkillPayload _payload;

        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        private Action<SkillImpactContext> OnArriveTrigger;

        public void Init(SkillPayload data, IProjectileMoveStrategy moveStretagy, Data.ProjectileData soData, Sprite sprite, SkillExecuter effectExecuter)
        {
            _effectExecuter = effectExecuter;
            _payload = data;
            _moveStretagy = moveStretagy;
            _renderer.sprite = sprite;
            _trigger = soData.EffectTrigger;
            _destroyTrigger = soData.DestroyTrigger;
            _soData = soData;
            OnArriveTrigger = (context) => CheckTrigger(EProjectileEffectTrigger.OnArrive, context.ImpactTarget);

            _moveStretagy.OnArrived += OnArriveTrigger;
        }

        private void Update()
        {
            //시간 초과
            if (_currentTime >= _soData.LifeTime)
            {
                CheckTrigger(EProjectileEffectTrigger.OnTimeOut, null);
                return;
            }

            if (IsActive == false) return;
            
            _currentTime += Time.deltaTime;
            _moveStretagy.Tick();
        }

        private void CheckTrigger(EProjectileEffectTrigger trigger, IDamageable target)
        {
            if (trigger == _trigger)
            {
                SkillImpactContext skillImpactContext = new SkillImpactContext(target, this.transform.position);

                _effectExecuter.Execute(skillImpactContext, _soData.ResolveData, _payload);
            }

            if(trigger == _destroyTrigger)
            {
                OnReturn?.Invoke(this);
            }
        }

        public void OnDespawn()
        {
            IsActive = false;
            _currentTime = 0;
            _moveStretagy.OnArrived -= OnArriveTrigger;
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