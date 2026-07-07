using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class RuntimeSkillBuild
    {
        public ActiveSkillData SourceSkill;
        public ActiveSkill Skill;
        public List<EffectBase> Effects = new List<EffectBase>();
        public List<RuntimeEffectSlot> Slots = new List<RuntimeEffectSlot>();
        private readonly Dictionary<int, SpawnEffect> _writableSpawns = new Dictionary<int, SpawnEffect>();

        public void SetEffects(List<EffectBase> effects)
        {
            Effects = effects;

            for (int i = 0; i < effects.Count; i++)
            {
                int rootIndex = i;
                EffectBase rootEffect = Effects[rootIndex];

                AddRootSlot(rootIndex, rootEffect);

                if (rootEffect is SpawnEffect spawnEffect)
                {
                    AddSpawnItemSlots(rootIndex, spawnEffect);
                }

                var slot = new RuntimeEffectSlot(Effects[i]);
                slot.Replace = (effect) => ReplaceEffect(rootIndex, effect);
                Slots.Add(slot);
            }
        }

        private void AddSpawnItemSlots(int rootIndex, SpawnEffect spawnEffect)
        {
            if (!(spawnEffect.Item is SummonItemData summonItem) || summonItem.Effects == null)
                return;

            var effects = summonItem.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                int innerIndex = i;
                EffectBase innerEffect = effects[innerIndex];

                var slot = new RuntimeEffectSlot(innerEffect);
                slot.Replace = copiedEffect =>
                {
                    SpawnEffect writableSpawnEffect = EnsureWritableSpawnEffect(rootIndex);
                    if (writableSpawnEffect?.Item is SummonItemData writableSummonItem)
                    {
                        writableSummonItem.Effects[innerIndex] = copiedEffect;
                    }
                };

                Slots.Add(slot);
            }
        }
        private SpawnEffect EnsureWritableSpawnEffect(int rootIndex)
        {
            if (_writableSpawns.TryGetValue(rootIndex, out var cached))
                return cached;

            SpawnEffect current = Effects[rootIndex] as SpawnEffect;

            if (current == null || current.Item == null)
                return null;

            SpawnEffect copiedSpawnEffect = UnityEngine.Object.Instantiate(current);

            if (current.Item is SummonItemData summonItem)
            {
                SummonItemData copiedSummonData = UnityEngine.Object.Instantiate(summonItem);
                copiedSummonData.Effects = new List<EffectBase>(summonItem.Effects);
                copiedSpawnEffect.Item = copiedSummonData;
            }

            Effects[rootIndex] = copiedSpawnEffect;

            _writableSpawns.Add(rootIndex, copiedSpawnEffect);
            return copiedSpawnEffect;
        }
        private void AddRootSlot(int index, EffectBase effect)
        {
            var slot = new RuntimeEffectSlot(effect);
            slot.Replace = copiedEffect => Effects[index] = copiedEffect;
            Slots.Add(slot);
        }
        public void ExtraEffect(EffectBase effectBase)
        {
            int index = Effects.Count;
            var slot = new RuntimeEffectSlot(effectBase);
            slot.Replace = (effect) => ReplaceEffect(index, effect);
            Slots.Add(slot);
            Effects.Add(effectBase);
        }
        public void ReplaceEffect(int index, EffectBase effect)
        {
            Effects[index] = effect;
        }
    }

    public class RuntimeEffectSlot
    {
        public Action<EffectBase> Replace;

        public EffectBase Original;
        public EffectBase Current;

        public RuntimeEffectSlot(EffectBase effectBase)
        {
            Original = effectBase;
        }

        public EffectBase GetWritableEffect()
        {
            if (Current == null)
            {
                Current = UnityEngine.Object.Instantiate(Original);
                Replace(Current);
            }

            return Current;
        }
    }
}