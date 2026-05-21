using Combat;
using Enemies;
using Entity;
using Stat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public interface IExecute
    {
        IEnumerator Execute(IReadOnlyList<Creature> targets);
        void AddEffect(EffectEntry effectEntry);
    }
    public abstract class ExecutionBase : IExecute
    {
        protected readonly ExecutionSystem _executionSystem;
        protected readonly Hero _owner;
        private SkillAnimationData _animaData;
        private ISpriteChanger _spriteChanger;
        protected readonly IVFXService _vfxService;
        protected readonly ICombatService _attackRegister;
        protected readonly List<EffectEntry> ExtraEffects = new List<EffectEntry>();

        public ExecutionBase(
            ExecutionSystem exectionSystem, SkillAnimationData animationData, ISpriteChanger spriteChanger, 
            IVFXService VFXService, ICombatService attackRegister, Hero owner)
        {
            _animaData = animationData;
            _spriteChanger = spriteChanger;
            _executionSystem = exectionSystem;
            _vfxService = VFXService;
            _attackRegister = attackRegister;
            _owner = owner;
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

        public void AddEffect(EffectEntry effectEntry)
        {
            ExtraEffects.Add(effectEntry);
        }
    }

    public class TargetMeleeExecution : ExecutionBase
    {
        public TargetMeleeExecution(ExecutionSystem exectionSystem, SkillAnimationData animationData, 
            ISpriteChanger spriteChanger, IVFXService VFXService, ICombatService attackRegister, Hero owner) : 
            base(exectionSystem, animationData, spriteChanger, VFXService, attackRegister, owner) { }
        public override IEnumerator Execute(IReadOnlyList<Creature> targets)
        {
            yield return SetReadyMotion();

            IDamageable target = SearchUtility.GetNearestTarget<IDamageable>(targets, _owner.Position);

            yield return SetExecutionMotion();

            if (_executionSystem.VFX)
                _vfxService.ShowVFX(_executionSystem.VFX);

            var stat = _owner.StatReadOnly;
            int damage = (int)stat.GetStat(EHeroStat.Damage);
            int fixPenetration = (int)stat.GetStat(EHeroStat.FixPenetration);
            int ratioPenetration = (int)stat.GetStat(EHeroStat.RatioPenetration);

            AttackPayload attackPayload = new AttackPayload(damage, fixPenetration, ratioPenetration);
            DamageContext dc = new DamageContext(attackPayload, target, _owner);

            for (int i = 0; i < _executionSystem.Effects.Count; i++)
            {

            }

            foreach (var effect in _executionSystem.Effects)
            {
                int chance = Random.Range(0, 100);

                if(effect.Chance >= chance)
                {
                    dc.RegisterEffect(effect.Effect);
                }
            }

            _attackRegister.RegisterAttack(dc);

            SetIdleMotion();
        }
    }
    public class ConeMeleeExecution : ExecutionBase
    {
        private float _angle;
        public ConeMeleeExecution(ExecutionSystem exectionSystem, SkillAnimationData animationData,
            ISpriteChanger spriteChanger, IVFXService VFXService, ICombatService attackRegister, Hero owner) :
            base(exectionSystem, animationData, spriteChanger, VFXService, attackRegister, owner)
        {
            if (exectionSystem is ConeMeleeAttack cone)
            {
                _angle = cone.Angle;
            }
            else
            {
                Debug.LogError($"Not Match Type {exectionSystem}");
            }
        }

        public override IEnumerator Execute(IReadOnlyList<Creature> targets)
        {
            yield return SetReadyMotion();

            IDamageable target = SearchUtility.GetNearestTarget<IDamageable>(targets, _owner.Position);

            Vector3 dir = (target.Position - _owner.Position).normalized;

            List<IDamageable> resultTargets = SearchUtility.GetConeTargets<IDamageable>(targets, _owner.Position, dir, _angle);

            yield return SetExecutionMotion();

            if (_executionSystem.VFX)
                _vfxService.ShowVFX(_executionSystem.VFX);

            var stat = _owner.StatReadOnly;
            int damage = (int)stat.GetStat(EHeroStat.Damage);
            int fixPenetration = (int)stat.GetStat(EHeroStat.FixPenetration);
            int ratioPenetration = (int)stat.GetStat(EHeroStat.RatioPenetration);

            foreach (var enemy in resultTargets)
            {
                AttackPayload attackPayload = new AttackPayload(damage, fixPenetration, ratioPenetration);
                DamageContext dc = new DamageContext(attackPayload, enemy, _owner);

                foreach (var effect in _executionSystem.Effects)
                {
                    int chance = Random.Range(0, 100);

                    if (effect.Chance >= chance)
                    {
                        dc.RegisterEffect(effect.Effect);
                    }
                }

                _attackRegister.RegisterAttack(dc);
            }

            SetIdleMotion();
        }
    }
    //public class TargetProjectile : IExecution
    //{
    //    public void Execution()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

}