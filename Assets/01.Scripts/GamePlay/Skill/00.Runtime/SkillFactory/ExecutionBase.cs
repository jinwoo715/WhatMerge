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
        protected readonly List<EffectBase> _effects;
        protected readonly Hero _owner;

        private readonly SkillAnimationData _animaData;
        private readonly ISpriteChanger _spriteChanger;
        protected readonly IVFXService _vfxService;
        protected readonly ICombatService _attackRegister;

        private ICombatant _appliedCombatant;

        protected ExecutionBase(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext)
        {
            _executionData = executionContext.ExecutionData;
            _animaData = executionContext.AnimationData;
            _owner = executionContext.Hero;
            _spriteChanger = executionContext.SpriteChanger;
            _effects = executionContext.Effects;

            _vfxService = runtimeContext.VFX;
            _attackRegister = runtimeContext.Combat;
        }
        public abstract IEnumerator Execute(IReadOnlyList<ICombatant> targets);

        protected IEnumerator SetReadyMotion()
        {
            if (_spriteChanger != null && _animaData != null)
            {
                _spriteChanger.SetSprite(_animaData.MotionReadyName);
                yield return new WaitForSeconds(_animaData.ReadyMotionTime);
            }
        }
        protected IEnumerator SetExecutionMotion()
        {
            if (_spriteChanger != null && _animaData != null)
            {
                _spriteChanger.SetSprite(_animaData.MotionName);
                Debug.Log(_animaData.MotionName);
                yield return new WaitForSeconds(_animaData.ExecutionMotionTime);
            }
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
            context.skillEffects = EffectRoller.GetConfirmEffects(_effects);
            _attackRegister.RegisterAttack(context);
        }
        protected ICombatant NearestTarget(IReadOnlyList<ICombatant> targets)
        {
            float distance = Mathf.Infinity;
            ICombatant nearCombatant = null;

            foreach (var combatant in targets)
            {
                float dis = Vector3.SqrMagnitude(combatant.Position - _owner.Position);

                if(dis < distance)
                {
                    distance = dis;
                    nearCombatant = combatant;
                }
            }

            return nearCombatant;
        }
    }
}
