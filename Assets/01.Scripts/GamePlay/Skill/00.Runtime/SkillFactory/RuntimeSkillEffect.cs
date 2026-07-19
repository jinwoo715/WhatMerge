using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class RuntimeSkillEffect
    {
        private class RuntimeEffectContainer : IEffectContainer
        {
            public List<EffectBase> GetEffects { get; set; }

            public RuntimeEffectContainer(List<EffectBase> effects)
            {
                GetEffects = effects;
            }
        }

        private List<EffectBase> _effects = new List<EffectBase>();

        private Dictionary<EffectBase, RuntimeEffectSlot> _effectSlots = new();
        private Dictionary<EffectBase, EffectBase> _singleEffectCopies = new();
        private Dictionary<ScriptableObject, IEffectContainer> _containers = new();

        public List<EffectBase> Effects => _effects;

        public void SetEffect(ExecutionData executionData)
        {
            _effects.Clear();
            _effectSlots.Clear();
            _singleEffectCopies.Clear();
            _containers.Clear();

            _effects = executionData.Effects != null ? CopyEffectList(executionData.Effects) : new List<EffectBase>();

            _containers.Add(executionData, new RuntimeEffectContainer(_effects));
        }

        private List<EffectBase> CopyEffectList(List<EffectBase> source)
        {
            if (source == null)
                return new List<EffectBase>();

            var copied = new List<EffectBase>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                EffectBase original = source[i];
                EffectBase runtime = CopyContainerEffect(original);

                copied.Add(runtime);
                AddEffectBase(copied, original, i);
            }

            return copied;
        }

        private EffectBase CopyContainerEffect(EffectBase effect)
        {
            if (effect is ProjectileSpawnEffect projectileSpawnEffect)
            {
                return CopyProjectileSpawnEffect(projectileSpawnEffect);
            }

            if (effect is SummonSpawnEffect summonSpawnEffect)
            {
                return CopySummonSpawnEffect(summonSpawnEffect);
            }

            if (effect is DurationEffect durationEffect)
            {
                return CopyDurationEffect(durationEffect);
            }

            return effect;
        }

        private ProjectileSpawnEffect CopyProjectileSpawnEffect(ProjectileSpawnEffect source)
        {
            ProjectileSpawnEffect copiedSpawnEffect = UnityEngine.Object.Instantiate(source);

            if (source.Projectile != null)
                copiedSpawnEffect.Projectile = CopyProjectileData(source.Projectile);

            return copiedSpawnEffect;
        }

        private SummonSpawnEffect CopySummonSpawnEffect(SummonSpawnEffect source)
        {
            SummonSpawnEffect copiedSpawnEffect = UnityEngine.Object.Instantiate(source);

            if (source.Move != null)
                copiedSpawnEffect.Move = UnityEngine.Object.Instantiate(source.Move);

            if (source.Execution != null)
                copiedSpawnEffect.Execution = CopySummonExecution(source.Execution);

            return copiedSpawnEffect;
        }

        private DurationEffect CopyDurationEffect(DurationEffect source)
        {
            DurationEffect copiedEffect = UnityEngine.Object.Instantiate(source);

            if (source.Effect != null)
                copiedEffect.Effect = CopySingleEffect(source.Effect) as DurationEffectBase;

            return copiedEffect;
        }

        private ProjectileDataBase CopyProjectileData(ProjectileDataBase source)
        {
            ProjectileDataBase copiedItem = UnityEngine.Object.Instantiate(source);
            copiedItem.Effects = CopyEffectList(source.Effects);

            AddContainer(source, copiedItem);
            return copiedItem;
        }

        private SummonExecutionData CopySummonExecution(SummonExecutionData source)
        {
            SummonExecutionData copiedExecution = UnityEngine.Object.Instantiate(source);

            if (source is SummonOnceExecution sourceOnceExecution
                && copiedExecution is SummonOnceExecution copiedOnceExecution
                && sourceOnceExecution.Effects != null)
            {
                copiedOnceExecution.Effects = CopySingleEffectList(sourceOnceExecution.Effects);
            }

            if (source is OnStayExecutionSummon sourceStayExecution
                && copiedExecution is OnStayExecutionSummon copiedStayExecution
                && sourceStayExecution.Effects != null)
            {
                copiedStayExecution.Effects = CopySingleEffectList(sourceStayExecution.Effects);
            }

            return copiedExecution;
        }

        private List<T> CopySingleEffectList<T>(List<T> source) where T : EffectBase
        {
            List<T> copied = new List<T>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                T original = source[i];
                copied.Add(original != null ? CopySingleEffect(original) as T : null);
            }

            return copied;
        }

        private EffectBase CopySingleEffect(EffectBase source)
        {
            EffectBase copiedEffect = CopyContainerEffect(source);

            if (copiedEffect == source)
                copiedEffect = UnityEngine.Object.Instantiate(source);

            AddSingleEffectCopy(source, copiedEffect);
            return copiedEffect;
        }

        public void AddEffect(ScriptableObject containerOrigin, EffectBase effectOrigin)
        {
            IEffectContainer container = GetEffectContainer(containerOrigin);
            EffectBase runtimeEffect = CopyContainerEffect(effectOrigin);

            container.GetEffects.Add(runtimeEffect);
            AddEffectBase(container.GetEffects, effectOrigin, container.GetEffects.Count - 1);
        }

        private void AddEffectBase(List<EffectBase> owner, EffectBase origin, int index)
        {
            if (origin == null)
                return;

            if(_effectSlots.TryGetValue(origin, out var value) || _singleEffectCopies.ContainsKey(origin))
            {
                Debug.LogError($"Already Added Effect : {origin.name}");
                return;
            }

            _effectSlots.Add(origin, new RuntimeEffectSlot(owner, origin, index));
        }

        public EffectBase GetEffectBase(EffectBase target)
        {
            if(!_effectSlots.TryGetValue(target, out var slot))
            {
                if (_singleEffectCopies.TryGetValue(target, out EffectBase copiedEffect))
                    return copiedEffect;

                throw new InvalidOperationException($"Not Registered Target : {target.name}");
            }

            return slot.GetRuntimeEffect();
        }

        private void AddSingleEffectCopy(EffectBase origin, EffectBase runtime)
        {
            if (origin == null || runtime == null)
                return;

            if (_effectSlots.ContainsKey(origin) || _singleEffectCopies.ContainsKey(origin))
            {
                Debug.LogError($"Already Added Effect : {origin.name}");
                return;
            }

            _singleEffectCopies.Add(origin, runtime);
        }

        private void AddContainer(ScriptableObject origin, IEffectContainer runtimeContainer)
        {
            if (origin == null || runtimeContainer == null)
                return;

            if (_containers.ContainsKey(origin))
            {
                Debug.LogError($"Already Added Container : {origin.name}");
                return;
            }

            _containers.Add(origin, runtimeContainer);
        }

        private IEffectContainer GetEffectContainer(ScriptableObject origin)
        {
            if(!_containers.TryGetValue(origin, out IEffectContainer target))
            {
                throw new InvalidOperationException($"Not Registered Container : {origin.name}");
            }

            return _containers[origin];
        }
    }
}
