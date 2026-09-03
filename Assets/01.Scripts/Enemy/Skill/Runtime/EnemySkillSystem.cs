using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies.Skills.Data;
using WhatMerge.Map;

namespace WhatMerge.Enemies.Skills.Runtime
{
    public sealed class EnemySkillSystem : IDisposable
    {
        private readonly Dictionary<Enemy, EnemySkillController> _controllers =
            new Dictionary<Enemy, EnemySkillController>();
        private readonly List<EnemySkillController> _activeControllers =
            new List<EnemySkillController>();
        private readonly HashSet<Enemy> _reservedEnemies = new HashSet<Enemy>();
        private readonly List<ProximityRequest> _proximityRequests =
            new List<ProximityRequest>();
        private readonly List<PendingActivation> _pendingActivations =
            new List<PendingActivation>();
        private readonly HashSet<EnemySkillData> _reportedUnsupportedSkills =
            new HashSet<EnemySkillData>();

        private EnemySkillCatalog _catalog;
        private IFieldEnemyService _fieldEnemyService;
        private EnemySkillEffectExecutor _effectExecutor;
        private IFatalStopService _fatalStopService;
        private bool _initialized;

        public void Init(
            EnemySkillCatalog catalog,
            IFieldEnemyService fieldEnemyService,
            IEnemySpawnService enemySpawnService,
            IPathProvider pathProvider,
            IVFXService vfxService,
            IFatalStopService fatalStopService)
        {
            if (_initialized)
                throw new InvalidOperationException($"{nameof(EnemySkillSystem)} is already initialized.");

            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _fieldEnemyService = fieldEnemyService ?? throw new ArgumentNullException(nameof(fieldEnemyService));
            if (enemySpawnService == null)
                throw new ArgumentNullException(nameof(enemySpawnService));
            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));
            if (vfxService == null)
                throw new ArgumentNullException(nameof(vfxService));
            _fatalStopService = fatalStopService ?? throw new ArgumentNullException(nameof(fatalStopService));

            EnemySkillValidator.ValidateOrThrow(_catalog);
            ValidateRuntimeSupport(_catalog);

            _effectExecutor = new EnemySkillEffectExecutor(
                enemySpawnService,
                fieldEnemyService,
                pathProvider,
                vfxService);

            _fieldEnemyService.OnSpawnEnemy += HandleEnemySpawned;
            _fieldEnemyService.OnEnemyDeath += HandleEnemyDeath;
            _fieldEnemyService.OnEnemyRemoved += HandleEnemyRemoved;
            _initialized = true;

            IReadOnlyList<Enemy> activeEnemies = _fieldEnemyService.GetAllFieldEnemy;
            for (int i = 0; i < activeEnemies.Count; i++)
                HandleEnemySpawned(activeEnemies[i]);
        }

        public void LateTick(float currentTime)
        {
            if (!_initialized)
                return;
            if (_fatalStopService.IsFatalStopped)
                return;
            if (float.IsNaN(currentTime) || float.IsInfinity(currentTime) || currentTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(currentTime), currentTime, "Current time must be finite and non-negative.");

            try
            {
                int controllerCount = _activeControllers.Count;
                for (int i = 0; i < controllerCount; i++)
                    CollectProximityRequests(_activeControllers[i], currentTime);

                _proximityRequests.Sort(CompareProximityRequests);
                for (int i = 0; i < _proximityRequests.Count; i++)
                    TryReserveProximityActivation(_proximityRequests[i]);

                for (int i = 0; i < _pendingActivations.Count; i++)
                    ExecutePendingActivation(_pendingActivations[i], currentTime);
            }
            catch (Exception exception)
            {
                _fatalStopService.FatalStop(exception, "Enemy skill proximity execution failed.");
            }
            finally
            {
                _pendingActivations.Clear();
                _proximityRequests.Clear();
                _reservedEnemies.Clear();
            }
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            _fieldEnemyService.OnSpawnEnemy -= HandleEnemySpawned;
            _fieldEnemyService.OnEnemyDeath -= HandleEnemyDeath;
            _fieldEnemyService.OnEnemyRemoved -= HandleEnemyRemoved;

            _controllers.Clear();
            _activeControllers.Clear();
            _reservedEnemies.Clear();
            _proximityRequests.Clear();
            _pendingActivations.Clear();
            _reportedUnsupportedSkills.Clear();
            _catalog = null;
            _fieldEnemyService = null;
            _effectExecutor = null;
            _fatalStopService = null;
            _initialized = false;
        }

        private void HandleEnemySpawned(Enemy enemy)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));
            if (!enemy.IsActive)
                throw new InvalidOperationException("An inactive enemy cannot receive a skill controller.");
            if (enemy.SkillSetUID <= 0)
                return;
            if (_fatalStopService.IsFatalStopped)
                return;
            if (_controllers.ContainsKey(enemy))
                throw new InvalidOperationException("The enemy already has a skill controller.");

            try
            {
                EnemySkillSetContainer skillSet = _catalog.GetSkillSet(enemy.SkillSetUID);
                EnemySkillController controller = new EnemySkillController(enemy, skillSet);
                ReportUnsupportedSkills(controller);

                _controllers.Add(enemy, controller);
                _activeControllers.Add(controller);
            }
            catch (Exception exception)
            {
                _fatalStopService.FatalStop(
                    exception,
                    $"Enemy skill initialization failed. EnemyUID:{enemy.UID}.");
            }
        }

        private void HandleEnemyDeath(Enemy enemy)
        {
            if (enemy == null || !_controllers.TryGetValue(enemy, out EnemySkillController controller))
                return;
            if (_fatalStopService.IsFatalStopped)
                return;

            try
            {
                float currentTime = Time.time;
                IReadOnlyList<EnemySkillRuntime> skills = controller.Skills;
                EnemySkillActivationContext context = new EnemySkillActivationContext(
                    enemy,
                    enemy.LifeCycleVersion,
                    enemy.LastActivePathPosition,
                    null,
                    0);

                for (int i = 0; i < skills.Count; i++)
                {
                    EnemySkillRuntime skill = skills[i];
                    if (!(skill.Data.Trigger is EnemyDeathTriggerData) || !skill.CanActivate(currentTime))
                        continue;

                    _effectExecutor.Execute(skill.Data, context);
                    skill.MarkActivated(currentTime);
                }
            }
            catch (Exception exception)
            {
                _fatalStopService.FatalStop(
                    exception,
                    $"Enemy death skill execution failed. EnemyUID:{enemy.UID}.");
            }
        }

        private void HandleEnemyRemoved(Enemy enemy)
        {
            if (enemy == null || !_controllers.TryGetValue(enemy, out EnemySkillController controller))
                return;

            _controllers.Remove(enemy);
            _activeControllers.Remove(controller);
        }

        private void CollectProximityRequests(
            EnemySkillController controller,
            float currentTime)
        {
            Enemy owner = controller.Owner;
            if (owner == null || !owner.IsActive)
                return;

            IReadOnlyList<EnemySkillRuntime> skills = controller.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                EnemySkillRuntime skill = skills[i];
                if (!(skill.Data.Trigger is EnemyProximityTriggerData)
                    || !skill.CanActivate(currentTime))
                {
                    continue;
                }

                _proximityRequests.Add(new ProximityRequest(
                    skill,
                    owner,
                    _proximityRequests.Count));
            }
        }

        private void TryReserveProximityActivation(ProximityRequest request)
        {
            Enemy owner = request.Owner;
            if (owner == null || !owner.IsActive || _reservedEnemies.Contains(owner))
                return;

            EnemyProximityTriggerData trigger =
                (EnemyProximityTriggerData)request.Skill.Data.Trigger;
            Enemy target = FindNearestTarget(owner, trigger);
            if (target == null)
                return;

            _reservedEnemies.Add(owner);
            _reservedEnemies.Add(target);
            _pendingActivations.Add(new PendingActivation(
                request.Skill,
                owner,
                owner.LifeCycleVersion,
                owner.CurrentPathPosition,
                target,
                target.LifeCycleVersion));
        }

        private Enemy FindNearestTarget(Enemy owner, EnemyProximityTriggerData trigger)
        {
            IReadOnlyList<Enemy> candidates =
                _fieldEnemyService.GetEnemiesByUID(trigger.TargetEnemyUID);
            float maximumSqrDistance = trigger.DetectionDistance * trigger.DetectionDistance;
            float nearestSqrDistance = float.PositiveInfinity;
            Enemy nearest = null;
            Vector2 ownerPosition = owner.Position;

            for (int i = 0; i < candidates.Count; i++)
            {
                Enemy candidate = candidates[i];
                if (candidate == null
                    || ReferenceEquals(candidate, owner)
                    || !candidate.IsActive
                    || _reservedEnemies.Contains(candidate))
                {
                    continue;
                }

                Vector2 delta = (Vector2)candidate.Position - ownerPosition;
                float sqrDistance = delta.sqrMagnitude;
                if (sqrDistance > maximumSqrDistance || sqrDistance >= nearestSqrDistance)
                    continue;

                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        private void ExecutePendingActivation(PendingActivation activation, float currentTime)
        {
            if (!IsValidHandle(activation.Owner, activation.OwnerLifeCycleVersion)
                || !IsValidHandle(activation.Target, activation.TargetLifeCycleVersion))
            {
                return;
            }

            EnemyProximityTriggerData trigger =
                (EnemyProximityTriggerData)activation.Skill.Data.Trigger;
            Vector2 delta = (Vector2)activation.Target.Position - (Vector2)activation.Owner.Position;
            if (delta.sqrMagnitude > trigger.DetectionDistance * trigger.DetectionDistance)
                return;

            EnemySkillActivationContext context = new EnemySkillActivationContext(
                activation.Owner,
                activation.OwnerLifeCycleVersion,
                activation.OwnerPathPosition,
                activation.Target,
                activation.TargetLifeCycleVersion);

            _effectExecutor.Execute(activation.Skill.Data, context);
            activation.Skill.MarkActivated(currentTime);
        }

        private void ReportUnsupportedSkills(EnemySkillController controller)
        {
            IReadOnlyList<EnemySkillData> unsupportedSkills = controller.UnsupportedSkills;
            for (int i = 0; i < unsupportedSkills.Count; i++)
            {
                EnemySkillData skill = unsupportedSkills[i];
                if (!_reportedUnsupportedSkills.Add(skill))
                    continue;

                Debug.LogWarning(
                    $"Enemy skill '{skill.Name}' uses trigger '{skill.Trigger.GetType().Name}', " +
                    "which is not included in the current enemy skill runtime.");
            }
        }

        private static bool IsValidHandle(Enemy enemy, int lifeCycleVersion)
        {
            return enemy != null
                && enemy.IsActive
                && enemy.LifeCycleVersion == lifeCycleVersion;
        }

        private static int CompareProximityRequests(ProximityRequest left, ProximityRequest right)
        {
            int priorityComparison = right.Skill.Data.ExecutionPolicy.Priority.CompareTo(
                left.Skill.Data.ExecutionPolicy.Priority);
            return priorityComparison != 0
                ? priorityComparison
                : left.CollectionOrder.CompareTo(right.CollectionOrder);
        }

        private static void ValidateRuntimeSupport(EnemySkillCatalog catalog)
        {
            for (int setIndex = 0; setIndex < catalog.SkillSets.Count; setIndex++)
            {
                EnemySkillSetContainer skillSet = catalog.SkillSets[setIndex];
                for (int skillIndex = 0; skillIndex < skillSet.Skills.Count; skillIndex++)
                {
                    EnemySkillData skill = skillSet.Skills[skillIndex];
                    if (!(skill.Trigger is EnemyDeathTriggerData)
                        && !(skill.Trigger is EnemyProximityTriggerData))
                    {
                        continue;
                    }

                    for (int actionIndex = 0; actionIndex < skill.Actions.Count; actionIndex++)
                    {
                        EnemySkillActionData action = skill.Actions[actionIndex];
                        if (action.Target != null && !(action.Target is TriggeredEnemyTargetData))
                        {
                            throw new InvalidOperationException(
                                $"Enemy skill '{skill.Name}' uses target '{action.Target.GetType().Name}', " +
                                "which is not included in the current enemy skill runtime.");
                        }

                        for (int effectIndex = 0; effectIndex < action.Effects.Count; effectIndex++)
                        {
                            EnemySkillEffectData effect = action.Effects[effectIndex];
                            if (effect is MergeEnemyEffectData
                                || effect is EnemySkillVFXEffectData)
                                continue;

                            if (effect is SpawnEnemyEffectData spawn)
                            {
                                if (spawn.SpawnInterval > 0f)
                                {
                                    throw new InvalidOperationException(
                                        $"Enemy skill '{skill.Name}' uses a SpawnInterval. " +
                                        "Delayed enemy skill spawning is not included in the current runtime.");
                                }

                                if (spawn.SpawnPositionType == EnemySpawnPositionType.AroundOwner)
                                {
                                    throw new InvalidOperationException(
                                        $"Enemy skill '{skill.Name}' uses AroundOwner spawning, " +
                                        "which cannot preserve an enemy path position.");
                                }

                                continue;
                            }

                            throw new InvalidOperationException(
                                $"Enemy skill '{skill.Name}' uses effect '{effect.GetType().Name}', " +
                                "which is not included in the current enemy skill runtime.");
                        }
                    }
                }
            }
        }

        private readonly struct PendingActivation
        {
            public EnemySkillRuntime Skill { get; }
            public Enemy Owner { get; }
            public int OwnerLifeCycleVersion { get; }
            public EnemyPathPosition OwnerPathPosition { get; }
            public Enemy Target { get; }
            public int TargetLifeCycleVersion { get; }

            public PendingActivation(
                EnemySkillRuntime skill,
                Enemy owner,
                int ownerLifeCycleVersion,
                EnemyPathPosition ownerPathPosition,
                Enemy target,
                int targetLifeCycleVersion)
            {
                Skill = skill;
                Owner = owner;
                OwnerLifeCycleVersion = ownerLifeCycleVersion;
                OwnerPathPosition = ownerPathPosition;
                Target = target;
                TargetLifeCycleVersion = targetLifeCycleVersion;
            }
        }

        private readonly struct ProximityRequest
        {
            public EnemySkillRuntime Skill { get; }
            public Enemy Owner { get; }
            public int CollectionOrder { get; }

            public ProximityRequest(
                EnemySkillRuntime skill,
                Enemy owner,
                int collectionOrder)
            {
                Skill = skill;
                Owner = owner;
                CollectionOrder = collectionOrder;
            }
        }
    }

    internal sealed class EnemySkillController
    {
        private readonly List<EnemySkillRuntime> _skills = new List<EnemySkillRuntime>();
        private readonly List<EnemySkillData> _unsupportedSkills = new List<EnemySkillData>();

        public Enemy Owner { get; }
        public IReadOnlyList<EnemySkillRuntime> Skills => _skills;
        public IReadOnlyList<EnemySkillData> UnsupportedSkills => _unsupportedSkills;

        public EnemySkillController(Enemy owner, EnemySkillSetContainer skillSet)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (skillSet == null)
                throw new ArgumentNullException(nameof(skillSet));

            for (int i = 0; i < skillSet.Skills.Count; i++)
            {
                EnemySkillData skill = skillSet.Skills[i];
                if (skill.Trigger is EnemyDeathTriggerData
                    || skill.Trigger is EnemyProximityTriggerData)
                {
                    _skills.Add(new EnemySkillRuntime(skill, i));
                }
                else
                {
                    _unsupportedSkills.Add(skill);
                }
            }

            _skills.Sort(CompareSkills);
        }

        private static int CompareSkills(EnemySkillRuntime left, EnemySkillRuntime right)
        {
            int priorityComparison =
                right.Data.ExecutionPolicy.Priority.CompareTo(left.Data.ExecutionPolicy.Priority);
            return priorityComparison != 0
                ? priorityComparison
                : left.DefinitionOrder.CompareTo(right.DefinitionOrder);
        }
    }

    internal sealed class EnemySkillRuntime
    {
        private int _activationCount;
        private float _nextAvailableTime;

        public EnemySkillData Data { get; }
        public int DefinitionOrder { get; }

        public EnemySkillRuntime(EnemySkillData data, int definitionOrder)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            DefinitionOrder = definitionOrder;
        }

        public bool CanActivate(float currentTime)
        {
            EnemySkillExecutionPolicy policy = Data.ExecutionPolicy;
            return (policy.MaxActivationCount == 0 || _activationCount < policy.MaxActivationCount)
                && currentTime >= _nextAvailableTime;
        }

        public void MarkActivated(float currentTime)
        {
            _activationCount++;
            _nextAvailableTime = currentTime + Data.ExecutionPolicy.Cooldown;
        }
    }

    internal readonly struct EnemySkillActivationContext
    {
        public Enemy Owner { get; }
        public int OwnerLifeCycleVersion { get; }
        public EnemyPathPosition OwnerPathPosition { get; }
        public Enemy TriggeredEnemy { get; }
        public int TriggeredEnemyLifeCycleVersion { get; }

        public EnemySkillActivationContext(
            Enemy owner,
            int ownerLifeCycleVersion,
            EnemyPathPosition ownerPathPosition,
            Enemy triggeredEnemy,
            int triggeredEnemyLifeCycleVersion)
        {
            Owner = owner;
            OwnerLifeCycleVersion = ownerLifeCycleVersion;
            OwnerPathPosition = ownerPathPosition;
            TriggeredEnemy = triggeredEnemy;
            TriggeredEnemyLifeCycleVersion = triggeredEnemyLifeCycleVersion;
        }
    }

    internal sealed class EnemySkillEffectExecutor
    {
        private readonly IEnemySpawnService _enemySpawnService;
        private readonly IFieldEnemyService _fieldEnemyService;
        private readonly IPathProvider _pathProvider;
        private readonly IVFXService _vfxService;

        public EnemySkillEffectExecutor(
            IEnemySpawnService enemySpawnService,
            IFieldEnemyService fieldEnemyService,
            IPathProvider pathProvider,
            IVFXService vfxService)
        {
            _enemySpawnService = enemySpawnService;
            _fieldEnemyService = fieldEnemyService;
            _pathProvider = pathProvider;
            _vfxService = vfxService;
        }

        public void Execute(EnemySkillData skill, EnemySkillActivationContext context)
        {
            for (int actionIndex = 0; actionIndex < skill.Actions.Count; actionIndex++)
            {
                EnemySkillActionData action = skill.Actions[actionIndex];
                Enemy target = ResolveTarget(action.Target, context);

                for (int effectIndex = 0; effectIndex < action.Effects.Count; effectIndex++)
                {
                    EnemySkillEffectData effect = action.Effects[effectIndex];
                    if (effect.Chance <= 0f || (effect.Chance < 1f && UnityEngine.Random.value > effect.Chance))
                        continue;

                    ShowVFX(effect, context, target);

                    if (effect is SpawnEnemyEffectData spawn)
                    {
                        ExecuteSpawn(spawn, context, target);
                    }
                    else if (effect is MergeEnemyEffectData merge)
                    {
                        ExecuteMerge(merge, context, target);
                        return;
                    }
                    else if (!(effect is EnemySkillVFXEffectData))
                    {
                        throw new NotSupportedException(
                            $"Effect type {effect.GetType().Name} is not supported by the enemy skill runtime.");
                    }
                }
            }
        }

        private void ExecuteSpawn(
            SpawnEnemyEffectData effect,
            EnemySkillActivationContext context,
            Enemy target)
        {
            EnemyPathPosition spawnPosition = ResolveSpawnPosition(effect, context, target);
            for (int i = 0; i < effect.Count; i++)
                _enemySpawnService.SpawnEnemy(effect.EnemyUID, spawnPosition);
        }

        private void ExecuteMerge(MergeEnemyEffectData effect, EnemySkillActivationContext context, Enemy target)
        {
            if (!IsValidHandle(context.Owner, context.OwnerLifeCycleVersion)
                || !IsValidHandle(target, context.TriggeredEnemyLifeCycleVersion))
            {
                return;
            }

            using (_fieldEnemyService.DeferEnemyCountNotifications())
            {
                _enemySpawnService.SpawnEnemy(effect.ResultEnemyUID, context.OwnerPathPosition);
                _enemySpawnService.DespawnEnemy(target);
                _enemySpawnService.DespawnEnemy(context.Owner);
            }
        }

        private EnemyPathPosition ResolveSpawnPosition(
            SpawnEnemyEffectData effect,
            EnemySkillActivationContext context,
            Enemy target)
        {
            switch (effect.SpawnPositionType)
            {
                case EnemySpawnPositionType.PathStart:
                    return EnemyPathPosition.Start;

                case EnemySpawnPositionType.Owner:
                    return context.OwnerPathPosition;

                case EnemySpawnPositionType.Target:
                    if (target == null)
                        throw new InvalidOperationException("Target spawning requires an Enemy target.");
                    return target.CurrentPathPosition;

                case EnemySpawnPositionType.RelativeToOwnerPath:
                    return EnemyPathPositionUtility.Offset(
                        _pathProvider,
                        context.OwnerPathPosition,
                        effect.PathDistanceOffset);

                default:
                    throw new NotSupportedException(
                        $"Spawn position type {effect.SpawnPositionType} is not supported by the enemy skill runtime.");
            }
        }

        private static Enemy ResolveTarget(EnemySkillTargetData targetData, EnemySkillActivationContext context)
        {
            if (targetData == null)
                return null;
            if (targetData is TriggeredEnemyTargetData)
                return context.TriggeredEnemy;

            throw new NotSupportedException(
                $"Target type {targetData.GetType().Name} is not supported by the enemy skill runtime.");
        }

        private void ShowVFX(
            EnemySkillEffectData effect,
            EnemySkillActivationContext context,
            Enemy target)
        {
            if (effect.VFX == null)
                return;

            Vector3 ownerPosition = EnemyPathPositionUtility.GetWorldPosition(
                _pathProvider,
                context.OwnerPathPosition);
            Vector3 targetPosition = target != null ? target.Position : ownerPosition;
            _vfxService.ShowVFX(effect.VFX, targetPosition, ownerPosition);
        }

        private static bool IsValidHandle(Enemy enemy, int lifeCycleVersion)
        {
            return enemy != null
                && enemy.IsActive
                && enemy.LifeCycleVersion == lifeCycleVersion;
        }
    }
}
