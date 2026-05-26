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
        public Action<IDamageable> OnExecuteEffect;
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

    public static class EffectRoller
    {
        public static List<EffectBase> GetConfirmEffects(List<EffectEntry> effects)
        {
            List<EffectBase> confirmedEffects = new List<EffectBase>();

            foreach (var effect in effects)
            {
                int chance = UnityEngine.Random.Range(0, 100);

                if (effect.Chance >= chance)
                {
                    confirmedEffects.Add(effect.Effect);
                }
            }

            return confirmedEffects;
        }
    }

    public class ProjectileEffectExecuter
    {
        private ICombatService _combatService;
        private ISummonProvider _summonProvider;

        private List<EffectEntry> _effects = new List<EffectEntry>();

        public ProjectileEffectExecuter(ICombatService combatService, ISummonProvider summonProvider, List<EffectEntry> effects)
        {
            _combatService = combatService;
            _summonProvider = summonProvider;
            _effects = effects;
        }
        public void Execute(IDamageable target)
        {
            var effects = EffectRoller.GetConfirmEffects(_effects);

            foreach (var effect in effects)
            {
                
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
        public bool IsActive { get; private set; }
        public event Action<ProjectileItem> OnReturn;

        public void Init(ProjectileEventContext data, ProjectileEffectExecuter effectExecuter, IMoveStretagy moveStretagy, ProjectileDataSO soData, Sprite sprite)
        {
            _data = data;
            _moveStretagy = moveStretagy;
            _renderer.sprite = sprite;
            _trigger = soData.EffectTrigger;
            _destroyTrigger = soData.DestroyTrigger;
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

            if(trigger == _destroyTrigger)
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
                Debug.Log(collision.name);
                if(collision.TryGetComponent<IDamageable>(out IDamageable target))
                {
                    CheckTrigger(EProjectileEffectTrigger.OnHit, target);
                }
            }
        }
    }
}