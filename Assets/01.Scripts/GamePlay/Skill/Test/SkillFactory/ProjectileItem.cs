using Combat;
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
        public List<EffectEntry> effects;
    }
    public interface IProjectileEventReceiver
    {
        void OnProjectileEvent(ProjectileEventContext context);
    }
    public class ProjectileEffectResolver : IProjectileEventReceiver
    {
        private readonly ICombatService _combatService;
        private readonly ISummonProvider _summonProvider;

        public void OnProjectileEvent(ProjectileEventContext context)
        {
            var stat = context.Attacker.StatReadOnly;
            int damage = (int)stat.GetStat(EHeroStat.Damage);
            int fixPenetration = (int)stat.GetStat(EHeroStat.FixPenetration);
            int ratioPenetration = (int)stat.GetStat(EHeroStat.RatioPenetration);

            AttackPayload attackPayload = new AttackPayload(damage, fixPenetration, ratioPenetration);
            DamageContext dc = new DamageContext(attackPayload, context.Target, context.Attacker);

            foreach (var effect in context.effects)
            {
                if (effect.IsUseable())
                {
                    if(effect.Effect is SummonEffect)
                    {
                        Debug.Log("소환!");
                    }
                    else
                    {
                        _combatService.RegisterAttack(dc);
                    }
                }
            }
        }
    }

    public class ProjectileEffectExecuter
    {
        private List<EffectBase> _effects = new List<EffectBase>();
        public void Init(EProjectileEffectTrigger trigger, List<EffectBase> effects)
        {
            _effects = effects;
            
        }
        public void Execute(IDamageable target)
        {

        }
    }

    public interface IProjectileMove
    {
        event Action OnArrived;
        void Init(ICreature destination, Transform owner);
        void Tick();
    }

    public class ProjectileItem : MonoBehaviour, IPooledItem<ProjectileItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private float _currentTime;

        private EProjectileEffectTrigger _trigger;
        private IProjectileMove _moveStretagy;
        private ProjectileEventContext _data;
        private ProjectileEffectExecuter _effectExecuter;
        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        public void Init(ProjectileEventContext data, ProjectileEffectExecuter effectExecuter, IProjectileMove moveStretagy, EProjectileEffectTrigger trigger, Sprite sprite)
        {
            _data = data;
            _moveStretagy = moveStretagy;
            _renderer.sprite = sprite;
            _trigger = trigger;
            _effectExecuter = effectExecuter;
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

        private void CheckTrigger(EProjectileEffectTrigger trigger, IDamageable damageable)
        {
            if (trigger == _trigger)
                _effectExecuter.Execute(damageable);
        }
        private void CheckDestroy(EProjectileEffectTrigger trigger, IDamageable damageable)
        {
            if (trigger == _trigger)
                OnReturn?.Invoke(this);
        }

        public void OnDespawn()
        {
            IsActive = false;
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
                    CheckTrigger(EProjectileEffectTrigger.OnHit, target);
                }
            }
        }
    }
}