using Combat;
using Enemies;
using Entity;
using Skill.Data;
using Skill.Projectile;
using Stat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public interface IExecute
    {
        IEnumerator Execute(IReadOnlyList<Creature> targets);
    }
    public abstract class ExecutionBase : IExecute
    {
        protected readonly ExecutionSystemData _executionSystem;
        protected readonly Hero _owner;
        private SkillAnimationData _animaData;
        private ISpriteChanger _spriteChanger;
        protected readonly IVFXService _vfxService;
        protected readonly ICombatService _attackRegister;
        protected readonly List<EffectBase> Effects = new List<EffectBase>();

        public ExecutionBase(ActiveSkillContext activeContext, SkillCommonContext commonContext)
        {
            _executionSystem = activeContext.System;
            _animaData = activeContext.AnimationData;
            Effects = activeContext.RuntimeEffects;
            _owner = activeContext.Hero;
            _spriteChanger = _owner.SpriteChanger;
            _attackRegister = commonContext.CombatService;
        }
        public abstract IEnumerator Execute(IReadOnlyList<Creature> targets);
        public IEnumerator SetReadyMotion()
        {
            _spriteChanger.SetSprite(_animaData.MotionReadyName);

            yield return new WaitForSeconds(_animaData.ExecutionMotionTime);
        }
        public IEnumerator SetExecutionMotion()
        {
            _spriteChanger.SetSprite(_animaData.MotionName);

            Debug.Log(_animaData.MotionName);

            yield return new WaitForSeconds(_animaData.ReadyMotionTime);
        }
        public void SetIdleMotion()
        {
            _spriteChanger.SetIdle();
        }
    }

    //단일 적용
    public class MeleeExecution : ExecutionBase
    {
        public MeleeExecution(ActiveSkillContext activeContext, SkillCommonContext commonContext) : base(activeContext, commonContext) { }
        public override IEnumerator Execute(IReadOnlyList<Creature> targets)
        {
            yield return SetReadyMotion();

            yield return SetExecutionMotion();

            if (_executionSystem.VFX)
                _vfxService.ShowVFX(_executionSystem.VFX);

            var stat = _owner.StatReadOnly;
            int damage = (int)stat.GetStat(EHeroStat.Damage);
            int fixPenetration = (int)stat.GetStat(EHeroStat.FixPenetration);
            int ratioPenetration = (int)stat.GetStat(EHeroStat.RatioPenetration);

            AttackPayload attackPayload = new AttackPayload(damage, fixPenetration, ratioPenetration);

            foreach (var target in targets)
            {
                ICreature creature = target as ICreature;
                DamageContext dc = new DamageContext(attackPayload, creature, _owner);
                dc.skillEffects = EffectRoller.GetConfirmEffects(Effects);
                _attackRegister.RegisterAttack(dc);
            }

            SetIdleMotion();
        }
    }

    //연속 적용

    //투사체 적용
    public class TargetProjectile : ExecutionBase
    {
        private IProjectileProvider _projectile;
        public TargetProjectile(ActiveSkillContext activeContext, SkillCommonContext commonContext) : base(activeContext, commonContext)
        {
            Debug.Log(commonContext);
            Debug.Log(commonContext.Projectile);
            _projectile = commonContext.Projectile;
        }

        public override IEnumerator Execute(IReadOnlyList<Creature> targets)
        {
            yield return SetReadyMotion();

            IDamageable target = SearchUtility.GetNearestTarget<IDamageable>(targets, _owner.Position);

            yield return SetExecutionMotion();

            var projectile = (_executionSystem as ProjectileSkill).ProjectileData;

            //TODO Projectile
            SkillPayload context = new SkillPayload();
            context.Attacker = _owner;
            context.Target = target;
            context.effects = EffectRoller.GetConfirmEffects(Effects);

            int damage = Mathf.RoundToInt(_owner.GetStat(EAttackStatType.Damage));
            context.payLoad = new AttackPayload(damage, 0, 0);

            _projectile.SpawnProjectile(projectile, context);

            SetIdleMotion();
        }
    }
}
