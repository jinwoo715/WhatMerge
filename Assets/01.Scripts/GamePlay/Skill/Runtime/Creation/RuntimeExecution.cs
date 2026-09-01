using Skill.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Projectiles.Data;
using WhatMerge.Summons.Data;

namespace Skill
{
    public class RuntimeExecution : IDisposable, IRuntimeEffectLifetime
    {
        private static long _nextRuntimeEffectInstanceId;
        private sealed class RuntimeEffectLease : IDisposable
        {
            private RuntimeExecution _owner;

            public RuntimeEffectLease(RuntimeExecution owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                RuntimeExecution owner = _owner;
                _owner = null;
                owner?.ReleaseReference();
            }
        }

        //Skill의 Root Effects
        private Dictionary<EffectBase, EffectBase> _runtimeEffects = new();

        //Effect List를 가진 Container들
        //Execution, ProjectileItem, SummonItem
        private Dictionary<ScriptableObject, IEffectContainer> _containers = new();
        private HashSet<UnityEngine.Object> _runtimeObjects = new();
        private HashSet<EffectBase> _copyingEffects = new();
        private bool _disposed;
        private bool _runtimeObjectsDestroyed;
        private int _referenceCount;

        public ExecutionData RuntimeExecutionData { get; private set; }

        public RuntimeExecution(ExecutionData originExecutionData)
        {
            if (originExecutionData == null)
                throw new ArgumentNullException(nameof(originExecutionData));

            _runtimeEffects.Clear();
            _containers.Clear();
            _runtimeObjects.Clear();
            _copyingEffects.Clear();
            _disposed = false;
            _runtimeObjectsDestroyed = false;
            _referenceCount = 1;

            if(originExecutionData.Effects == null)
                throw new InvalidOperationException($"Not Effect Execution : {originExecutionData.name}");

            try
            {
                RuntimeExecutionData = CreateRuntimeObject(originExecutionData);
                RuntimeExecutionData.Effects = CopyEffectList(originExecutionData.Effects);

                AddContainer(originExecutionData, RuntimeExecutionData);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private T CreateRuntimeObject<T>(T origin) where T : UnityEngine.Object
        {
            T runtimeObject = UnityEngine.Object.Instantiate(origin);
            _runtimeObjects.Add(runtimeObject);
            return runtimeObject;
        }

        private List<EffectBase> CopyEffectList(List<EffectBase> source)
        {
            if (source == null)
                return new List<EffectBase>();

            var copied = new List<EffectBase>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                EffectBase original = source[i];
                EffectBase runtimeEffect = CreateRuntimeEffect(original);
                
                copied.Add(runtimeEffect);
            }

            return copied;
        }
        //Projectile, Summon, DurationEffect는 내부적으로 적용 Effect를 참조하고 있기 때문에, 내부 Effect까지 복사해야함.
        private EffectBase CreateRuntimeEffect(EffectBase origin)
        {
            if (origin == null)
            {
                throw new InvalidOperationException("Effect reference is null.");
            }

            if (_runtimeEffects.ContainsKey(origin))
            {
                throw new InvalidOperationException($"Already Added Effect : {origin.name}");
            }

            if (!_copyingEffects.Add(origin))
            {
                throw new InvalidOperationException($"Circular Effect Reference : {origin.name}");
            }

            try
            {
                EffectBase runtimeEffect = origin switch
                {
                    ProjectileSpawnEffect projectileSpawnEffect => CreateRuntimeProjectileSpawnEffect(projectileSpawnEffect),
                    SummonSpawnEffect summonSpawnEffect => CreateSummonSpawnEffect(summonSpawnEffect),
                    DurationEffect durationEffect => CreateDurationEffect(durationEffect),
                    RangeEffect rangeEffect => CreateRangeEffect(rangeEffect),
                    _ => CreateRuntimeObject(origin)
                };

                runtimeEffect.RuntimeEffectInstanceId = Interlocked.Increment(
                    ref _nextRuntimeEffectInstanceId);

                _runtimeEffects.Add(origin, runtimeEffect);
                return runtimeEffect;
            }
            finally
            {
                _copyingEffects.Remove(origin);
            }
        }
        private EffectBase CreateRangeEffect(RangeEffect origin)
        {
            RangeEffect effect = CreateRuntimeObject(origin);
            effect.Effects = CopyEffectList(origin.Effects);

            AddContainer(origin, effect);

            return effect;
        }
        private ProjectileSpawnEffect CreateRuntimeProjectileSpawnEffect(ProjectileSpawnEffect origin)
        {
            ProjectileSpawnEffect createSpawnEffect = CreateRuntimeObject(origin);
            ProjectileDataBase spawnItem = CreateRuntimeObject(origin.Projectile);

            createSpawnEffect.Projectile = spawnItem;
            spawnItem.Effects = CopyEffectList(origin.Projectile.Effects);

            AddContainer(origin.Projectile, spawnItem);

            return createSpawnEffect;
        }
        private SummonSpawnEffect CreateSummonSpawnEffect(SummonSpawnEffect origin)
        {
            SummonSpawnEffect summonSpawnEffect = CreateRuntimeObject(origin);
            SummonExecutionData executionData = CreateRuntimeObject(origin.Execution);

            summonSpawnEffect.Execution = executionData;
            executionData.SetEffects(CopyEffectList(origin.Execution.GetEffects));

            AddContainer(origin.Execution, executionData);

            return summonSpawnEffect;
        }
        private DurationEffect CreateDurationEffect(DurationEffect origin)
        {
            DurationEffect durationEffect = CreateRuntimeObject(origin);
            durationEffect.SetEffectList(CopyEffectList(origin.GetEffects));

            AddContainer(origin, durationEffect);

            return durationEffect;
        }
        public void InsertExtraEffect(ScriptableObject insertContainerOrigin, EffectBase effectOrigin)
        {
            IEffectContainer container = GetEffectContainer(insertContainerOrigin);

            EffectBase runtimeEffect = CreateRuntimeEffect(effectOrigin);

            container.AddEffect(runtimeEffect);
        }
        private void AddContainer(ScriptableObject origin, IEffectContainer runtimeContainer)
        {
            if (origin == null || runtimeContainer == null)
                return;

            if (_containers.ContainsKey(origin))
            {
                throw new InvalidOperationException($"Already Added Container : {origin.name}");
            }

            _containers.Add(origin, runtimeContainer);
        }

        //쿼리
        public EffectBase GetRuntimeEffect(EffectBase origin)
        {
            if(!_runtimeEffects.TryGetValue(origin, out var effect))
            {
                throw new InvalidOperationException($"Not Registered Effect : {origin.name}");
            }

            return effect;
        }
        private IEffectContainer GetEffectContainer(ScriptableObject origin)
        {
            if(_containers.TryGetValue(origin, out IEffectContainer target))
            {
                return target;
            }
            else
            {
                throw new InvalidOperationException($"Not Registered Container : {origin.name}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ReleaseReference();
        }

        public IDisposable Retain()
        {
            if (_runtimeObjectsDestroyed || _referenceCount <= 0)
                throw new ObjectDisposedException(nameof(RuntimeExecution));

            _referenceCount++;
            return new RuntimeEffectLease(this);
        }

        private void ReleaseReference()
        {
            if (_referenceCount <= 0)
                throw new InvalidOperationException("Runtime effect reference count is already zero.");

            _referenceCount--;

            if (_referenceCount != 0)
                return;

            _runtimeObjectsDestroyed = true;

            foreach (UnityEngine.Object runtimeObject in _runtimeObjects)
            {
                if (runtimeObject != null)
                    UnityEngine.Object.Destroy(runtimeObject);
            }

            _runtimeObjects.Clear();
            _runtimeEffects.Clear();
            _containers.Clear();
            _copyingEffects.Clear();
            RuntimeExecutionData = null;
        }
    }
}
