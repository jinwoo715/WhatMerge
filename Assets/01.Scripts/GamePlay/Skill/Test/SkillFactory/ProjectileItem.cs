using Combat;
using Enemies;
using Entity;
using Skill.Data;
using Stat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class ProjectileEventContext
    {
        public Hero Attacker;
        public IDamageable Target;
        public List<EffectBase> effects;
        public AttackPayload payLoad;
    }

    public class ProjectileImpactContext
    {
        public IDamageable HitTarget;
        public Vector3 Position;
        public ProjectileImpactContext(IDamageable target, Vector3 position)
        {
            HitTarget = target;
            Position = position;
        }
    }

    public class ProjectileEffectExecuter
    {
        private ICombatService _combatService;
        private ProjectileEventContext _context;
        private TargetResolveData _targetResolveType;

        public ProjectileEffectExecuter(ICombatService combatService, TargetResolveData targetResolveType, ProjectileEventContext context)
        {
            _combatService = combatService;
            _targetResolveType = targetResolveType;
            _context = context;
        }
        public void Execute(ProjectileImpactContext impactContext)
        {
            if(_targetResolveType.Type == ETargetResolveType.Single)
            {
                DamageContext context = new DamageContext(_context.payLoad, impactContext.HitTarget, _context.Attacker);
                context.skillEffects = _context.effects;
                _combatService.RegisterAttack(context);
            }
            else if(_targetResolveType.Type == ETargetResolveType.Area)
            {
                var enemies = SearchUtility.GetNearAll2DTargets<Enemy>(impactContext.Position, _targetResolveType.Radius, LayerMask.GetMask("Enemy"));

                foreach (var enemy in enemies)
                {
                    DamageContext context = new DamageContext(_context.payLoad, enemy, _context.Attacker);
                    context.skillEffects = _context.effects;
                    _combatService.RegisterAttack(context);
                }
            }
        }
    }



    public class ProjectileItem : MonoBehaviour, IPooledItem<ProjectileItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private float _currentTime;

        private EProjectileEffectTrigger _trigger;
        private EProjectileEffectTrigger _destroyTrigger;
        private IMoveStretagy _moveStretagy;
        
        private ProjectileEventContext _data;
        private ProjectileEffectExecuter _effectExecuter;

        private ProjectileDataSO _soData;

        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        private Action<ProjectileImpactContext> OnCheckTrigger;

        public void Init(ProjectileEventContext data, ProjectileEffectExecuter effectExecuter, IMoveStretagy moveStretagy, ProjectileDataSO soData, Sprite sprite)
        {
            _data = data;
            _moveStretagy = moveStretagy;
            _renderer.sprite = sprite;
            _trigger = soData.EffectTrigger;
            _destroyTrigger = soData.DestroyTrigger;
            _effectExecuter = effectExecuter;
            _soData = soData;

            OnCheckTrigger = (context) => CheckTrigger(EProjectileEffectTrigger.OnArrive, context);

            _moveStretagy.OnArrived += OnCheckTrigger;
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

        private void CheckTrigger(EProjectileEffectTrigger trigger, ProjectileImpactContext context)
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
            _moveStretagy.OnArrived -= OnCheckTrigger;
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

                    CheckTrigger(EProjectileEffectTrigger.OnHit, new ProjectileImpactContext(target, target.Position));
                }
            }
        }
    }
}