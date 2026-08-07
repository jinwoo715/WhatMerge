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
        IEnumerator Execute(IReadOnlyList<ICombatant> targets);
    }

    public abstract class ExecutionBase : IExecute
    {
        protected readonly List<EffectBase> _effects;
        protected readonly Hero _owner;

        private readonly SkillAnimationData _animaData;
        private readonly ISpriteChanger _spriteChanger;
        protected readonly ICombatService _attackRegister;

        private readonly int SkillUid;
        private readonly int OwnerSpawnIndex;
        private readonly IRuntimeEffectLifetime _effectLifetime;
        private Enemy _previousEnemy;
        private int _previousEnemyLifeCycleVersion;

        protected ExecutionBase(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext)
        {
            _animaData = executionContext.AnimationData;
            _owner = executionContext.Hero;
            _spriteChanger = executionContext.SpriteChanger;
            _effects = executionContext.Effects;

            _attackRegister = runtimeContext.Combat;

            SkillUid = executionContext.SkillUid;
            OwnerSpawnIndex = executionContext.Hero.SpawnIndex;
            _effectLifetime = executionContext.EffectLifetime;
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

        protected void ApplyEffectsToTarget(ICombatant target)
        {
            if (target == null || !target.IsActive || _owner == null || _attackRegister == null)
            {
                return;
            }

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
                return _previousEnemy;
            }

            ICombatant target = NearestTarget(targets);
            RememberPrimaryTarget(target);
            return target;
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
    }
}
