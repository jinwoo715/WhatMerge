using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace Skill
{
    public interface IExecute
    {
        IEnumerator Execute(IReadOnlyList<ICombatant> targets);
    }

    public abstract class ExecutionBase : IExecute
    {
        protected readonly ExecutionData _executionData;
        protected readonly Hero _owner;
        private readonly SkillAnimationData _animaData;
        private readonly ISpriteChanger _spriteChanger;
        protected readonly IVFXService _vfxService;
        protected readonly ICombatService _attackRegister;
        protected readonly List<EffectBase> Effects;

        protected ExecutionBase(ActiveSkillContext activeContext, SkillCommonContext commonContext)
        {
            _executionData = activeContext.Execution;
            _animaData = activeContext.AnimationData;
            _owner = activeContext.Hero;
            _spriteChanger = activeContext.SpriteChanger;
            _attackRegister = commonContext.CombatService;
        }
        public abstract IEnumerator Execute(IReadOnlyList<ICombatant> targets);
        
        protected IEnumerator SetReadyMotion()
        {
            if (_spriteChanger != null && _animaData != null)
            {
                _spriteChanger.SetSprite(_animaData.MotionReadyName);
                yield return new WaitForSeconds(_animaData.ExecutionMotionTime);
            }
        }
        protected IEnumerator SetExecutionMotion()
        {
            if (_spriteChanger != null && _animaData != null)
            {
                _spriteChanger.SetSprite(_animaData.MotionName);
                Debug.Log(_animaData.MotionName);
                yield return new WaitForSeconds(_animaData.ReadyMotionTime);
            }
        }

        protected void Execute()
        {

        }

        public void SetIdleMotion()
        {
            _spriteChanger?.SetIdle();
        }

        protected void ShowExecutionVfx()
        {
            if (_vfxService != null && _executionData?.VFX != null)
            {
                _vfxService.ShowVFX(_executionData.VFX);
            }
        }
        protected void ApplyEffectsToTarget(ICombatant target)
        {
            if (target == null || !target.IsActive || _owner == null || _attackRegister == null)
            {
                return;
            }

            DamageContext context = new DamageContext(_owner.CreateAttackPayload(), target, _owner);
            context.skillEffects = EffectRoller.GetConfirmEffects(Effects);
            _attackRegister.RegisterAttack(context);
        }
        protected void ApplyEffectsToTargets(IEnumerable<ICombatant> targets)
        {
            if (targets == null)
            {
                return;
            }

            foreach (ICombatant target in targets)
            {
                ApplyEffectsToTarget(target);
            }
        }
        protected List<ICombatant> GetActiveTargets(IReadOnlyList<ICombatant> targets)
        {
            List<ICombatant> activeTargets = new List<ICombatant>();

            if (targets == null)
            {
                return activeTargets;
            }

            foreach (ICombatant target in targets)
            {
                if (target != null && target.IsActive)
                {
                    activeTargets.Add(target);
                }
            }

            return activeTargets;
        }
    }
}
