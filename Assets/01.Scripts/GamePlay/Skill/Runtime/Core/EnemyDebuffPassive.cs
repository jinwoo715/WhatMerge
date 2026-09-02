using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Combat.Effects;
using WhatMerge.Enemies;
using WhatMerge.Heros;

namespace Skill.Data
{
    internal sealed class DebuffApplication : IDisposable
    {
        private readonly Enemy _target;
        private readonly IReadOnlyList<DebuffData> _effects;
        private int _appliedCount;
        private bool _isApplied;
        private bool _isDisposed;

        public DebuffApplication(Enemy target, IReadOnlyList<DebuffData> effects)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        public void Apply()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(DebuffApplication));
            if (_isApplied)
                return;
            if (!_target.IsActive)
                throw new InvalidOperationException("A passive debuff cannot be applied to an inactive enemy.");

            try
            {
                for (int i = 0; i < _effects.Count; i++)
                {
                    DebuffData effect = _effects[i];
                    _appliedCount = i + 1;
                    _target.StatModifier.AddMultiplier(effect.StatType, -effect.ReductionRatio);
                }

                _isApplied = true;
            }
            catch
            {
                RollbackFailedApply();
                throw;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Exception firstException = null;

            for (int i = _appliedCount - 1; i >= 0; i--)
            {
                try
                {
                    DebuffData effect = _effects[i];
                    _target.StatModifier.AddMultiplier(effect.StatType, effect.ReductionRatio);
                }
                catch (Exception exception)
                {
                    CaptureCleanupException(ref firstException, exception);
                }
            }

            _appliedCount = 0;
            _isApplied = false;

            if (firstException != null)
                throw firstException;
        }

        private void RollbackFailedApply()
        {
            for (int i = _appliedCount - 1; i >= 0; i--)
            {
                try
                {
                    DebuffData effect = _effects[i];
                    _target.StatModifier.AddMultiplier(effect.StatType, effect.ReductionRatio);
                }
                catch (Exception cleanupException)
                {
                    Debug.LogException(cleanupException);
                }
            }

            _appliedCount = 0;
            _isApplied = false;
        }

        private static void CaptureCleanupException(
            ref Exception firstException,
            Exception exception)
        {
            if (firstException == null)
                firstException = exception;
            else
                Debug.LogException(exception);
        }
    }

    public abstract class EnemyDebuffPassive : PassiveSkill
    {
        private readonly Dictionary<Enemy, DebuffApplication> _applications = new();
        private readonly List<Enemy> _releaseBuffer = new();
        private readonly IReadOnlyList<DebuffData> _effects;
        private readonly IFatalStopService _fatalStop;
        private readonly string _skillName;
        private bool _isApplied;

        protected IFieldEnemyService FieldEnemyService { get; }
        protected bool IsApplied => _isApplied;
        protected IEnumerable<Enemy> AppliedTargets => _applications.Keys;

        protected EnemyDebuffPassive(
            IFieldEnemyService fieldEnemyService,
            IReadOnlyList<DebuffData> effects,
            IFatalStopService fatalStop,
            string skillName)
        {
            FieldEnemyService = fieldEnemyService
                ?? throw new ArgumentNullException(nameof(fieldEnemyService));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));
            _skillName = string.IsNullOrWhiteSpace(skillName) ? "Unnamed passive debuff" : skillName;

            ValidateEffects(_effects);
        }

        public sealed override void Apply()
        {
            if (_isApplied)
                return;

            _isApplied = true;

            try
            {
                Bind();
                ApplyInitialTargets();
            }
            catch
            {
                TryCleanupAfterFailure();
                throw;
            }
        }

        public sealed override void Release()
        {
            if (!_isApplied && _applications.Count == 0)
                return;

            _isApplied = false;
            Exception firstException = null;

            try
            {
                Unbind();
            }
            catch (Exception exception)
            {
                CaptureCleanupException(ref firstException, exception);
            }

            _releaseBuffer.Clear();
            foreach (Enemy target in _applications.Keys)
                _releaseBuffer.Add(target);

            for (int i = _releaseBuffer.Count - 1; i >= 0; i--)
            {
                try
                {
                    ReleaseTarget(_releaseBuffer[i]);
                }
                catch (Exception exception)
                {
                    CaptureCleanupException(ref firstException, exception);
                }
            }

            _releaseBuffer.Clear();

            if (firstException != null)
                throw firstException;
        }

        protected virtual void Bind() { }
        protected virtual void Unbind() { }
        protected abstract void ApplyInitialTargets();

        protected bool HasTarget(Enemy target)
        {
            return target != null && _applications.ContainsKey(target);
        }

        protected void ApplyTarget(Enemy target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (!target.IsActive || _applications.ContainsKey(target))
                return;

            DebuffApplication application = new DebuffApplication(target, _effects);
            bool added = false;

            try
            {
                application.Apply();
                _applications.Add(target, application);
                added = true;
                target.OnActiveOff += HandleTargetActiveOff;
            }
            catch
            {
                if (added)
                    _applications.Remove(target);

                target.OnActiveOff -= HandleTargetActiveOff;

                try
                {
                    application.Dispose();
                }
                catch (Exception cleanupException)
                {
                    Debug.LogException(cleanupException);
                }

                throw;
            }
        }

        protected void ReleaseTarget(Enemy target)
        {
            if (target == null || !_applications.Remove(target, out DebuffApplication application))
                return;

            target.OnActiveOff -= HandleTargetActiveOff;
            application.Dispose();
        }

        protected void AbortTickAfterFailure()
        {
            TryCleanupAfterFailure();
        }

        protected void HandleExternalFailure(Exception exception, string operation)
        {
            TryCleanupAfterFailure();
            _fatalStop.FatalStop(
                exception,
                $"Passive debuff '{_skillName}' {operation} failed.");
        }

        private void HandleTargetActiveOff(ICombatant combatant)
        {
            if (combatant is not Enemy target)
                return;

            try
            {
                ReleaseTarget(target);
            }
            catch (Exception exception)
            {
                HandleExternalFailure(exception, "target cleanup");
                throw;
            }
        }

        private void TryCleanupAfterFailure()
        {
            try
            {
                Release();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(new InvalidOperationException(
                    $"Passive debuff '{_skillName}' cleanup also failed.",
                    cleanupException));
            }
        }

        private static void ValidateEffects(IReadOnlyList<DebuffData> effects)
        {
            if (effects.Count == 0)
                throw new InvalidOperationException("A passive debuff requires at least one effect.");

            HashSet<EnemyStatType> statTypes = new();

            for (int i = 0; i < effects.Count; i++)
            {
                DebuffData effect = effects[i]
                    ?? throw new InvalidOperationException($"Passive debuff effect at index {i} is null.");

                if (!Enum.IsDefined(typeof(EnemyStatType), effect.StatType))
                    throw new InvalidOperationException($"Unsupported enemy stat type: {effect.StatType}.");
                if (effect.StatType == EnemyStatType.MaxHP)
                    throw new InvalidOperationException("Passive MaxHP reduction is not supported.");
                if (!statTypes.Add(effect.StatType))
                    throw new InvalidOperationException($"Duplicate passive debuff stat: {effect.StatType}.");
                if (float.IsNaN(effect.ReductionRatio)
                    || float.IsInfinity(effect.ReductionRatio)
                    || effect.ReductionRatio <= 0f
                    || effect.ReductionRatio > 1f)
                {
                    throw new InvalidOperationException(
                        $"Passive debuff reduction ratio must be greater than 0 and at most 1. " +
                        $"Current value: {effect.ReductionRatio}.");
                }
            }
        }

        private static void CaptureCleanupException(
            ref Exception firstException,
            Exception exception)
        {
            if (firstException == null)
                firstException = exception;
            else
                Debug.LogException(exception);
        }
    }

    public sealed class AllEnemyDebuffPassive : EnemyDebuffPassive
    {
        public AllEnemyDebuffPassive(
            IFieldEnemyService fieldEnemyService,
            IReadOnlyList<DebuffData> effects,
            IFatalStopService fatalStop,
            string skillName)
            : base(fieldEnemyService, effects, fatalStop, skillName)
        {
        }

        protected override void Bind()
        {
            FieldEnemyService.OnSpawnEnemy += HandleEnemySpawn;
        }

        protected override void Unbind()
        {
            FieldEnemyService.OnSpawnEnemy -= HandleEnemySpawn;
        }

        protected override void ApplyInitialTargets()
        {
            IReadOnlyList<Enemy> targets = FieldEnemyService.GetAllFieldEnemy;

            for (int i = 0; i < targets.Count; i++)
                ApplyTarget(targets[i]);
        }

        private void HandleEnemySpawn(Enemy target)
        {
            if (!IsApplied)
                return;

            try
            {
                ApplyTarget(target);
            }
            catch (Exception exception)
            {
                HandleExternalFailure(exception, "spawn application");
                throw;
            }
        }
    }

    public sealed class NearEnemyDebuffPassive : EnemyDebuffPassive
    {
        private readonly Hero _owner;
        private readonly float _radiusSquared;
        private readonly HashSet<Enemy> _nextTargets = new();
        private readonly List<Enemy> _exitedTargets = new();

        public NearEnemyDebuffPassive(
            IFieldEnemyService fieldEnemyService,
            Hero owner,
            IReadOnlyList<DebuffData> effects,
            float radius,
            IFatalStopService fatalStop,
            string skillName)
            : base(fieldEnemyService, effects, fatalStop, skillName)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be positive and finite.");

            _radiusSquared = radius * radius;
        }

        public override void Tick(float deltaTime)
        {
            if (!IsApplied)
                return;

            try
            {
                RefreshTargets();
            }
            catch
            {
                AbortTickAfterFailure();
                throw;
            }
        }

        protected override void ApplyInitialTargets()
        {
            RefreshTargets();
        }

        private void RefreshTargets()
        {
            _nextTargets.Clear();
            IReadOnlyList<Enemy> fieldEnemies = FieldEnemyService.GetAllFieldEnemy;
            Vector3 ownerPosition = _owner.Position;

            for (int i = 0; i < fieldEnemies.Count; i++)
            {
                Enemy target = fieldEnemies[i];
                if (target == null || !target.IsActive)
                    continue;

                if ((target.Position - ownerPosition).sqrMagnitude <= _radiusSquared)
                    _nextTargets.Add(target);
            }

            foreach (Enemy target in _nextTargets)
            {
                if (!HasTarget(target))
                    ApplyTarget(target);
            }

            _exitedTargets.Clear();
            foreach (Enemy target in AppliedTargets)
            {
                if (!_nextTargets.Contains(target))
                    _exitedTargets.Add(target);
            }

            for (int i = 0; i < _exitedTargets.Count; i++)
                ReleaseTarget(_exitedTargets[i]);

            _exitedTargets.Clear();
        }
    }
}
