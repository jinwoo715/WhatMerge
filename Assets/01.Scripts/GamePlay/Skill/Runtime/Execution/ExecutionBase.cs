using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;
using WhatMerge.Heros;

namespace Skill
{
    public interface IExecute
    {
        float BaseAnimationDuration { get; }
        float ChargeTime { get; }
        IEnumerator Execute(IReadOnlyList<ICombatant> targets, float animationTimeScale);
    }

    public abstract class ExecutionBase : IExecute
    {
        protected readonly List<EffectBase> _effects;
        protected readonly Hero _owner;

        private readonly SkillAnimationData _animaData;
        private readonly ISpriteChanger _spriteChanger;
        private readonly VFXData _executionVfx;
        private readonly IVFXService _vfxService;
        protected readonly ICombatService _attackRegister;

        private readonly int SkillUid;
        private readonly int OwnerSpawnIndex;
        private readonly IRuntimeEffectLifetime _effectLifetime;
        private Enemy _previousEnemy;
        private int _previousEnemyLifeCycleVersion;

        public virtual float BaseAnimationDuration => _animaData == null ? 0f : _animaData.ReadyMotionTime + _animaData.ExecutionMotionTime;
        public float ChargeTime { get; }

        protected ExecutionBase(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext)
        {
            _animaData = executionContext.AnimationData;
            _owner = executionContext.Hero;
            _spriteChanger = executionContext.SpriteChanger;
            _effects = executionContext.Effects;
            _executionVfx = executionContext.ExecutionData.ExecutionVFX;
            ChargeTime = ValidateChargeTime(executionContext.ChargeTime);

            _attackRegister = runtimeContext.Combat;
            _vfxService = runtimeContext.VFX;

            SkillUid = executionContext.SkillUid;
            OwnerSpawnIndex = executionContext.Hero.SpawnIndex;
            _effectLifetime = executionContext.EffectLifetime;

            ValidateAnimationData(_animaData);
        }
        public abstract IEnumerator Execute(IReadOnlyList<ICombatant> targets, float animationTimeScale);

        protected IEnumerator SetReadyMotion(float animationTimeScale)
        {
            if (_spriteChanger != null && _animaData != null)
            {
                _spriteChanger.SetSprite(_animaData.MotionReadyName);

                float duration = _animaData.ReadyMotionTime * animationTimeScale;
                if (duration > 0f)
                    yield return new WaitForSeconds(duration);
            }
        }
        protected IEnumerator SetExecutionMotion(
            float animationTimeScale,
            ICombatant target = null)
        {
            ShowExecutionVFX(target);

            if (_spriteChanger != null && _animaData != null)
            {
                _spriteChanger.SetSprite(_animaData.MotionName);

                float duration = _animaData.ExecutionMotionTime * animationTimeScale;
                if (duration > 0f)
                    yield return new WaitForSeconds(duration);
            }
        }

        private void ShowExecutionVFX(ICombatant target)
        {
            if (_executionVfx == null)
                return;
            if (_vfxService == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(ExecutionData.ExecutionVFX)} requires an {nameof(IVFXService)}.");
            }

            Vector3 targetPosition = target?.Position ?? _owner.Position;
            _vfxService.ShowVFX(_executionVfx, targetPosition, _owner.Position);
        }
        protected IEnumerator WaitForCharge()
        {
            if (ChargeTime > 0f)
                yield return new WaitForSeconds(ChargeTime);
        }
        public void SetIdleMotion()
        {
            _spriteChanger?.SetIdle();
        }

        protected void ApplyEffectsToTarget(ICombatant target)
        {
            if (target == null || !target.IsActive || _owner == null || _attackRegister == null)
                return;

            DamageContext context = new DamageContext(
                _owner.CreateAttackPayload(),
                target,
                _owner,
                SkillUid,
                OwnerSpawnIndex,
                effectLifetime: _effectLifetime);
            context.Effects = _effects;
            _attackRegister.RegisterAttack(context);
        }
        protected ICombatant NearestTarget(IReadOnlyList<ICombatant> targets)
        {
            if (targets == null)
            {
                return null;
            }

            float distance = Mathf.Infinity;
            ICombatant nearCombatant = null;

            foreach (var combatant in targets)
            {
                if (combatant == null || !combatant.IsActive)
                {
                    continue;
                }

                float dis = Vector3.SqrMagnitude(combatant.Position - _owner.Position);

                if (dis < distance)
                {
                    distance = dis;
                    nearCombatant = combatant;
                }
            }

            return nearCombatant;
        }

        protected ICombatant SelectPrimaryTarget(IReadOnlyList<ICombatant> targets)
        {
            if (IsPreviousEnemyAvailable(targets))
            {
                LookAtTarget(_previousEnemy);
                return _previousEnemy;
            }

            ICombatant target = NearestTarget(targets);
            RememberPrimaryTarget(target);
            LookAtTarget(target);
            return target;
        }

        protected void LookAtTarget(ICombatant target)
        {
            if (target != null)
                _spriteChanger?.LookAt(target.Position);
        }

        private bool IsPreviousEnemyAvailable(IReadOnlyList<ICombatant> targets)
        {
            if (_previousEnemy == null
                || !_previousEnemy.IsActive
                || _previousEnemy.LifeCycleVersion != _previousEnemyLifeCycleVersion
                || targets == null)
            {
                return false;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (ReferenceEquals(targets[i], _previousEnemy))
                {
                    return true;
                }
            }

            return false;
        }

        private void RememberPrimaryTarget(ICombatant target)
        {
            if (target is Enemy enemy)
            {
                _previousEnemy = enemy;
                _previousEnemyLifeCycleVersion = enemy.LifeCycleVersion;
                return;
            }

            _previousEnemy = null;
            _previousEnemyLifeCycleVersion = 0;
        }

        private static void ValidateAnimationData(SkillAnimationData animationData)
        {
            if (animationData == null)
                return;

            ValidateAnimationTime(animationData.ReadyMotionTime, nameof(animationData.ReadyMotionTime));
            ValidateAnimationTime(animationData.ExecutionMotionTime, nameof(animationData.ExecutionMotionTime));
        }

        private static void ValidateAnimationTime(float value, string fieldName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    fieldName,
                    value,
                    "Animation time must be a finite number greater than or equal to zero.");
            }
        }

        private static float ValidateChargeTime(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(SkillExecutionContext.ChargeTime),
                    value,
                    "Charge time must be a finite number greater than or equal to zero.");
            }

            return value;
        }
    }
}
