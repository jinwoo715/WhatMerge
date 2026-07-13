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
        private Dictionary<ScriptableObject, IEffectContainer> _containers = new();

        public List<EffectBase> Effects => _effects;

        public void SetEffect(ExecutionData executionData)
        {
            _effects.Clear();
            _effectSlots.Clear();
            _containers.Clear();

            _effects = executionData.Effects != null ? CopyEffectList(executionData.Effects) : new List<EffectBase>();

            _containers.Add(executionData, new RuntimeEffectContainer(_effects));
        }

        private List<EffectBase> CopyEffectList(List<EffectBase> source)
        {
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
            if (effect is SpawnEffect spawnEffect)
            {
                return CopySpawnEffect(spawnEffect);
            }

            return effect;
        }

        private SpawnEffect CopySpawnEffect(SpawnEffect source)
        {
            SpawnEffect copiedSpawnEffect = UnityEngine.Object.Instantiate(source);

            if (source.Item != null)
            {
                SpawnItemData copiedItem = UnityEngine.Object.Instantiate(source.Item);
                copiedItem.Effects = CopyEffectList(source.Item.Effects);
                copiedSpawnEffect.Item = copiedItem;

                _containers.Add(source.Item, copiedItem);
            }

            return copiedSpawnEffect;
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
            if(_effectSlots.TryGetValue(origin, out var value))
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
                throw new InvalidOperationException($"Not Registered Target : {target.name}");
            }

            return slot.GetRuntimeEffect();
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
