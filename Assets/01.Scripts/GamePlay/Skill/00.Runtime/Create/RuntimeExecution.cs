using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class RuntimeExecution : IDisposable
    {
        //Skill의 Root Effects
        private Dictionary<EffectBase, EffectBase> _runtimeEffects = new();

        //Effect List를 가진 Container들
        //Execution, ProjectileItem, SummonItem
        private Dictionary<ScriptableObject, IEffectContainer> _containers = new();
        private HashSet<UnityEngine.Object> _runtimeObjects = new();
        private bool _disposed;

        public ExecutionData RuntimeExecutionData { get; private set; }

        public RuntimeExecution(ExecutionData originExecutionData)
        {
            _runtimeEffects.Clear();
            _containers.Clear();
            _runtimeObjects.Clear();
            _disposed = false;

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
            if (_runtimeEffects.ContainsKey(origin))
            {
                throw new InvalidOperationException($"Already Added Effect : {origin.name}");
            }

            EffectBase runtimeEffect = null;

            switch (origin)
            {
                case ProjectileSpawnEffect projectileSpawnEffect:
                    runtimeEffect = CreateRuntimeProjectileSpawnEffect(projectileSpawnEffect);
                    break;
                case SummonSpawnEffect summonSpawnEffect:
                    runtimeEffect = CreateSummonSpawnEffect(summonSpawnEffect);
                    break;
                case DurationEffect durationEffect:
                    runtimeEffect = CreateDurationEffect(durationEffect);
                    break;
                case RangeEffect rangeEffect:
                    runtimeEffect = CreateRangeEffect(rangeEffect);
                    break;
                default:
                    runtimeEffect = CreateRuntimeObject(origin);
                    break;
            }
            
            _runtimeEffects.Add(origin, runtimeEffect);
            return runtimeEffect;
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

            foreach (UnityEngine.Object runtimeObject in _runtimeObjects)
            {
                if (runtimeObject != null)
                    UnityEngine.Object.Destroy(runtimeObject);
            }

            _runtimeObjects.Clear();
            _runtimeEffects.Clear();
            _containers.Clear();
            RuntimeExecutionData = null;
        }
    }
}
