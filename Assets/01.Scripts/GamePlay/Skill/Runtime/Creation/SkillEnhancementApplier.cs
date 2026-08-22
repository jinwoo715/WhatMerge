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

        public static void ApplyActivationChanceEnhance(
            Dictionary<ActiveSkillData, ActiveSkill> activeSkills,
            Queue<ActivationChanceEnhanceData> enhancers)
        {
            foreach (ActivationChanceEnhanceData enhancer in enhancers)
            {
                ActiveSkill activeSkill = GetRuntimeActiveSkill(activeSkills, enhancer.TargetSkill, enhancer);
                activeSkill.AddActivationChance(enhancer.AddChance);
            }
        }

        public static void ApplyTriggerRequirementReduction(
            Dictionary<ActiveSkillData, ActiveSkill> activeSkills,
            Queue<TriggerRequirementReductionData> enhancers)
        {
            foreach (TriggerRequirementReductionData enhancer in enhancers)
            {
                ActiveSkill activeSkill = GetRuntimeActiveSkill(activeSkills, enhancer.TargetSkill, enhancer);

                if (activeSkill.Trigger is not ITriggerRequirementModifier modifier)
                {
                    throw new InvalidOperationException(
                        $"Enhancer '{GetDataName(enhancer)}' targets skill " +
                        $"'{GetDataName(enhancer.TargetSkill)}' whose trigger does not support requirement reduction.");
                }

                modifier.AddRequirementReductionRatio(enhancer.ReductionRatio);
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

        private static ActiveSkill GetRuntimeActiveSkill(
            Dictionary<ActiveSkillData, ActiveSkill> activeSkills,
            ActiveSkillData targetSkill,
            SkillBaseData enhancer)
        {
            if (targetSkill == null)
            {
                throw new InvalidOperationException(
                    $"Enhancer '{GetDataName(enhancer)}' has no target skill.");
            }

            if (!activeSkills.TryGetValue(targetSkill, out ActiveSkill activeSkill))
            {
                throw new InvalidOperationException(
                    $"Enhancer '{GetDataName(enhancer)}' targets missing active skill " +
                    $"'{GetDataName(targetSkill)}'. " +
                    "The target skill is not included in this skill set or level.");
            }

            return activeSkill;
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
