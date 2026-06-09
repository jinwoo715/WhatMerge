using Combat;
using Enemies;
using Entity;
using Skill.Data;
using Stat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Projectile
{
    public class ProjectileItem : MonoBehaviour, IPooledItem<ProjectileItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private float _currentTime;

        private EProjectileEffectTrigger _trigger;
        private EProjectileEffectTrigger _destroyTrigger;
        private IProjectileMoveStrategy _moveStretagy;
        
        private SkillPayload _data;
        private SkillExecuter _effectExecuter;

        private Data.ProjectileData _soData;

        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        private Action<SkillImpactContext> OnArriveTrigger;

        public void Initialize(SkillExecuter effectExecuter)
        {
            _effectExecuter = effectExecuter;
        }

        public void Init(SkillPayload data, IProjectileMoveStrategy moveStretagy, Data.ProjectileData soData, Sprite sprite)
        {
            _data = data;
            _moveStretagy = moveStretagy;
            _renderer.sprite = sprite;
            _trigger = soData.EffectTrigger;
            _destroyTrigger = soData.DestroyTrigger;
            _soData = soData;
            OnArriveTrigger = (context) => CheckTrigger(EProjectileEffectTrigger.OnArrive, context);

            _moveStretagy.OnArrived += OnArriveTrigger;
            _effectExecuter.SetData(soData.ResolveData, data);
        }

        private void Update()
        {
            //시간 초과
            if (_currentTime >= 3.0f)
            {
                OnReturn?.Invoke(this);
                return;
            }

            if (IsActive == false) return;
            
            _currentTime += Time.deltaTime;
            _moveStretagy.Tick();
        }

        private void CheckTrigger(EProjectileEffectTrigger trigger, SkillImpactContext context)
        {
            if (trigger == _trigger)
            {
                _effectExecuter.Execute(context);
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

                    CheckTrigger(EProjectileEffectTrigger.OnHit, new SkillImpactContext(target, target.Position));
                }
            }
        }
    }
}