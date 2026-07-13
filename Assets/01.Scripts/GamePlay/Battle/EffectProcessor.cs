using Skill;
using Skill.Data;
using Skill.Summon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;

namespace WhatMerge.Combat
{
    public class EffectProcessor
    {
        private readonly DamageCalculator _damageCalculator;
        private readonly IEffectRoutineRunner _effectRoutineRunner;
        private readonly IVFXService _vfx;
        private readonly List<IEffectHandler> _handlers;

        public event Action<Vector3, int> OnApplyDamage;

        public EffectProcessor(DamageCalculator damageCalculator, IVFXService vfx, ISummonProvider summonProvider, IBuffRegister buffRegister)
        {
            _damageCalculator = damageCalculator;
            _vfx = vfx;
            _effectRoutineRunner = buffRegister as IEffectRoutineRunner;

            DamageEffectHandler damageEffectHandler = new DamageEffectHandler(_damageCalculator);
            damageEffectHandler.OnApplyDamage += HandleApplyDamage;

            _handlers = new List<IEffectHandler>
            {
                damageEffectHandler,
                new SpawnEffectHandler(summonProvider),
                new BuffEffectHandler(buffRegister)
            };
        }

        public void Process(DamageContext damageContext)
        {
            ProcessEffects(damageContext.skillEffects, damageContext);
        }

        private void ProcessEffects(List<EffectBase> effects, DamageContext damageContext)
        {
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

                ShowEffectVFX(effect, damageContext);

                if (TryHandleEffect(effect, damageContext))
                    continue;

                switch (effect)
                {
                    case DotEffect dot:
                        ProcessDotEffect(dot, damageContext);
                        break;
                    case SlowEffect slow:
                        ProcessEnemyTimedMultiplier(damageContext.Target, EnemyStatType.MoveSpeed, -slow.SlowRatio, slow.Duration);
                        break;
                    case ArmorReduction armorReduction:
                        ProcessEnemyTimedMultiplier(damageContext.Target, EnemyStatType.Armor, -armorReduction.Value, armorReduction.Duration);
                        break;
                    case ElementEffect element:
                        ProcessElementEffect(damageContext.Target, element);
                        break;
                }
            }
        }

        private bool TryHandleEffect(EffectBase effect, DamageContext damageContext)
        {
            foreach (var handler in _handlers)
            {
                if (!handler.CanHandle(effect))
                    continue;

                handler.Handle(effect, damageContext);
                return true;
            }

            return false;
        }

        private void HandleApplyDamage(Vector3 position, int damage)
        {
            OnApplyDamage?.Invoke(position, damage);
        }

        private void ShowEffectVFX(EffectBase effect, DamageContext damageContext)
        {
            if (_vfx == null || effect.VFX == null || damageContext.Target == null || damageContext.Attacker == null)
                return;

            _vfx.ShowEffect(effect.VFX.VFXName, damageContext.Target.Position, damageContext.Attacker.Position);
        }

        private void ApplyDamage(IDamageable damageable, int appliedDamage)
        {
            if (damageable == null || !damageable.IsActive || appliedDamage <= 0)
                return;

            damageable.TakeDamage(new AttackResultPayload(appliedDamage));
            OnApplyDamage?.Invoke(damageable.Position, appliedDamage);
        }

        private void ProcessDotEffect(DotEffect dot, DamageContext damageContext)
        {
            if (damageContext.Target is not IDamageable damageable)
                return;

            if (_effectRoutineRunner == null || dot.Duration <= 0f || dot.IntervalTime <= 0f)
            {
                ApplyDamage(damageable, _damageCalculator.CalculateDotDamage(damageable, dot));
                return;
            }

            int lifeCycleVersion = GetLifeCycleVersion(damageable);
            _effectRoutineRunner.RunEffect(CoProcessDotEffect(damageable, dot, lifeCycleVersion));
        }

        private IEnumerator CoProcessDotEffect(IDamageable damageable, DotEffect dot, int lifeCycleVersion)
        {
            float elapsedTime = 0f;

            while (elapsedTime + dot.IntervalTime <= dot.Duration + Mathf.Epsilon && IsSameLifeCycleActive(damageable, lifeCycleVersion))
            {
                yield return new WaitForSeconds(dot.IntervalTime);
                elapsedTime += dot.IntervalTime;

                if (!IsSameLifeCycleActive(damageable, lifeCycleVersion))
                    break;

                ApplyDamage(damageable, _damageCalculator.CalculateDotDamage(damageable, dot));
            }
        }

        private void ProcessEnemyTimedMultiplier(ICombatant target, EnemyStatType statType, float multiplier, float duration)
        {
            if (target is not Enemy enemy)
                return;

            if (_effectRoutineRunner == null || duration <= 0f)
                return;

            _effectRoutineRunner.RunEffect(CoProcessEnemyTimedMultiplier(enemy, enemy.LifeCycleVersion, statType, multiplier, duration));
        }

        private IEnumerator CoProcessEnemyTimedMultiplier(Enemy enemy, int lifeCycleVersion, EnemyStatType statType, float multiplier, float duration)
        {
            enemy.AddMultiplier(statType, multiplier);
            yield return new WaitForSeconds(duration);

            if (enemy.LifeCycleVersion == lifeCycleVersion)
                enemy.AddMultiplier(statType, -multiplier);
        }

        private void ProcessElementEffect(ICombatant target, ElementEffect element)
        {
            if (target is not Enemy enemy)
                return;

            if (_effectRoutineRunner == null || element.Duration <= 0f)
                return;

            _effectRoutineRunner.RunEffect(CoProcessElementEffect(enemy, enemy.LifeCycleVersion, element));
        }

        private IEnumerator CoProcessElementEffect(Enemy enemy, int lifeCycleVersion, ElementEffect element)
        {
            enemy.Status.AddStatus(element.Attribute);
            yield return new WaitForSeconds(element.Duration);

            if (enemy.LifeCycleVersion == lifeCycleVersion)
                enemy.Status.RemoveStatus(element.Attribute);
        }

        private int GetLifeCycleVersion(IDamageable damageable)
        {
            return damageable is Enemy enemy ? enemy.LifeCycleVersion : 0;
        }

        private bool IsSameLifeCycleActive(IDamageable damageable, int lifeCycleVersion)
        {
            if (!damageable.IsActive)
                return false;

            return damageable is not Enemy enemy || enemy.LifeCycleVersion == lifeCycleVersion;
        }
    }
}
