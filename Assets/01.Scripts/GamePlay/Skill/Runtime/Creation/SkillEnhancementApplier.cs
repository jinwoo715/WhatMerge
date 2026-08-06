using System;
using System.Collections.Generic;
using Skill.Data;

namespace Skill
{
    public class SkillEnhancementApplier
    {
        public static void ApplyExtraEffect(Dictionary<ActiveSkillData, RuntimeExecution> activeSkillDatas, Queue<ExtraEffectData> enhancers)
        {
            foreach (var enhancer in enhancers)
            {
                var effectContainer = GetRuntimeExecution(activeSkillDatas, enhancer.TargetSkill, enhancer);
                effectContainer.InsertExtraEffect(enhancer.EffectContainer, enhancer.Effect);
            }
        }
        public static void ApplyStatEnhance(Dictionary<ActiveSkillData, RuntimeExecution> activeSkillDatas, Queue<EffectValueEnhanceData> enhancers)
        {
            foreach (var enhancer in enhancers)
            {
                var effectContainer = GetRuntimeExecution(activeSkillDatas, enhancer.TargetSkill, enhancer);
                var effect = effectContainer.GetRuntimeEffect(enhancer.TargetEffect);
                ValidateStatKey(enhancer, effect);
                effect.AddStat(enhancer.TargetStatKey, enhancer.AddValue);
            }
        }
        public static void ApplyChanceEnhance(Dictionary<ActiveSkillData, RuntimeExecution> activeSkillDatas, Queue<EffectChanceEnhanceData> enhancers)
        {
            foreach (var enhancer in enhancers)
            {
                var value = GetRuntimeExecution(activeSkillDatas, enhancer.TargetSkill, enhancer);
                var effect = value.GetRuntimeEffect(enhancer.TargetEffect);
                effect.AddChance(enhancer.AddChance);
            }
        }
        public static RuntimeExecution GetRuntimeExecution(
          Dictionary<ActiveSkillData, RuntimeExecution> activeSkillDatas,
          ActiveSkillData targetSkill,
          SkillBaseData enhancer)
        {
            if (targetSkill == null)
            {
                throw new InvalidOperationException($"Enhancer '{GetDataName(enhancer)}' has no target skill.");
            }

            if (!activeSkillDatas.TryGetValue(targetSkill, out RuntimeExecution runtimeEffect))
            {
                throw new InvalidOperationException(
                    $"Enhancer '{GetDataName(enhancer)}' targets missing active skill '{GetDataName(targetSkill)}'. " +
                    "The target skill is not included in this skill set or level.");
            }

            return runtimeEffect;
        }
        private static void ValidateStatKey(EffectValueEnhanceData enhancer, EffectBase effect)
        {
            string targetKey = enhancer.TargetStatKey;
            IReadOnlyList<EffectStatDefinition> stats = effect.GetEnhanceableStats();

            if (!string.IsNullOrWhiteSpace(targetKey) && stats != null)
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    if (stats[i].Key == targetKey)
                        return;
                }
            }

            throw new InvalidOperationException(
                $"Enhancer '{GetDataName(enhancer)}' targets unsupported stat key " +
                $"'{targetKey ?? "null"}' on effect '{enhancer.TargetEffect?.name ?? "null"}'.");
        }
        public static string GetDataName(SkillBaseData data)
        {
            return data == null ? "null" : $"{data.name}(UID:{data.UID})";
        }
    }
}
