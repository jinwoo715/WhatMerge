using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Heros;
using WhatMerge.Projectiles.Data;
using WhatMerge.Summons.Data;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "UpgradeSet", menuName = "Skill/UpgradeSet", order = 0)]
    public class SkillSetContainer : ScriptableObject
    {
        public int UID;
        public List<HeroGradeSkillSet> GradeSets;

        public List<HeroSkillSet> GetSets(HeroGrade grade, int level)
        {
            HeroGradeSkillSet gradeSet = GetGradeSet(grade);

            if (gradeSet.Sets == null)
                throw new InvalidOperationException(
                    $"Skill set container '{name}' has no set list for grade {grade}.");

            List<HeroSkillSet> sets = new List<HeroSkillSet>();
            int previousLevel = int.MinValue;

            for (int i = 0; i < gradeSet.Sets.Count; i++)
            {
                HeroSkillSet set = gradeSet.Sets[i];

                if (set == null)
                {
                    throw new InvalidOperationException(
                        $"Skill set container '{name}', grade {grade} has a null entry at index {i}.");
                }

                if (set.Level < previousLevel)
                {
                    throw new InvalidOperationException(
                        $"Skill set container '{name}', grade {grade} must be ordered by level. " +
                        $"Index {i - 1}: {previousLevel}, index {i}: {set.Level}.");
                }

                previousLevel = set.Level;

                if (set.Level <= level)
                    sets.Add(set);
            }

            return sets;
        }

        public HeroGradeSkillSet GetGradeSet(HeroGrade grade)
        {
            if (GradeSets == null)
                throw new InvalidOperationException($"Skill set container '{name}' has no grade set list.");

            HeroGradeSkillSet result = null;

            for (int i = 0; i < GradeSets.Count; i++)
            {
                HeroGradeSkillSet candidate = GradeSets[i];

                if (candidate == null)
                {
                    throw new InvalidOperationException(
                        $"Skill set container '{name}' has a null grade entry at index {i}.");
                }

                if (candidate.Grade != grade)
                    continue;

                if (result != null)
                {
                    throw new InvalidOperationException(
                        $"Skill set container '{name}' has duplicate grade group {grade}.");
                }

                result = candidate;
            }

            return result ?? throw new InvalidOperationException(
                $"Skill set container '{name}' has no grade group {grade}.");
        }
    }

    [System.Serializable]
    public class HeroGradeSkillSet
    {
        public HeroGrade Grade;
        public List<HeroSkillSet> Sets;
    }

    [System.Serializable]
    public class HeroSkillSet
    {
        public int Level;
        public SkillBaseData Skill;
    }

    public static class SkillSetValidator
    {
        private sealed class EffectGraph
        {
            public readonly HashSet<EffectBase> Effects = new();
            public readonly HashSet<ScriptableObject> Containers = new();
        }

        public static IReadOnlyList<string> Validate(
            SkillSetContainer container,
            HeroData heroData,
            int maxLevel)
        {
            List<string> errors = new List<string>();

            if (container == null)
            {
                errors.Add("SkillSetContainer is null.");
                return errors;
            }

            if (heroData == null)
            {
                errors.Add($"HeroData for container '{container.name}' is null.");
                return errors;
            }

            if (maxLevel < 1)
                errors.Add($"MaxLevel must be greater than zero. Value: {maxLevel}.");
            if (container.UID != heroData.UID)
            {
                errors.Add(
                    $"Container UID {container.UID} does not match HeroData UID {heroData.UID}.");
            }

            if (heroData.BaseGrade < HeroGrade.D || heroData.BaseGrade > HeroGrade.B)
                errors.Add($"Hero UID {heroData.UID} BaseGrade must be D, C, or B.");
            if ((int)heroData.BaseGrade + 2 > (int)HeroGrade.S)
                errors.Add($"Hero UID {heroData.UID} exceeds S grade after two evolutions.");

            ValidateGradeGroups(container, heroData, maxLevel, errors);
            return errors;
        }

        public static void ValidateOrThrow(
            SkillSetContainer container,
            HeroData heroData,
            int maxLevel)
        {
            IReadOnlyList<string> errors = Validate(container, heroData, maxLevel);
            if (errors.Count == 0)
                return;

            throw new InvalidOperationException(
                $"SkillSet validation failed for '{container?.name ?? "null"}':\n- " +
                string.Join("\n- ", errors));
        }

        private static void ValidateGradeGroups(
            SkillSetContainer container,
            HeroData heroData,
            int maxLevel,
            List<string> errors)
        {
            if (container.GradeSets == null)
            {
                errors.Add("GradeSets is null.");
                return;
            }

            if (container.GradeSets.Count != 3)
                errors.Add($"GradeSets must contain exactly three groups. Count: {container.GradeSets.Count}.");

            HashSet<HeroGrade> grades = new HashSet<HeroGrade>();
            for (int i = 0; i < container.GradeSets.Count; i++)
            {
                HeroGradeSkillSet group = container.GradeSets[i];
                if (group == null)
                {
                    errors.Add($"Grade group at index {i} is null.");
                    continue;
                }

                if (!grades.Add(group.Grade))
                    errors.Add($"Grade group {group.Grade} is duplicated.");

                int expectedOffset = (int)group.Grade - (int)heroData.BaseGrade;
                if (expectedOffset < 0 || expectedOffset > 2)
                    errors.Add($"Grade group {group.Grade} is unreachable from {heroData.BaseGrade}.");

                ValidateGroup(container, group, maxLevel, errors);
            }

            for (int offset = 0; offset < 3; offset++)
            {
                HeroGrade expected = (HeroGrade)((int)heroData.BaseGrade + offset);
                if (!grades.Contains(expected))
                    errors.Add($"Required grade group {expected} is missing.");
            }
        }

        private static void ValidateGroup(
            SkillSetContainer container,
            HeroGradeSkillSet group,
            int maxLevel,
            List<string> errors)
        {
            string prefix = $"{container.name}/{group.Grade}";
            if (group.Sets == null || group.Sets.Count == 0)
            {
                errors.Add($"{prefix}: skill entries are null or empty.");
                return;
            }

            Dictionary<ActiveSkillData, int> activeUnlockLevels = new();
            Dictionary<ActiveSkillData, EffectGraph> activeGraphs = new();
            int previousLevel = int.MinValue;
            int basicAttackCount = 0;

            for (int i = 0; i < group.Sets.Count; i++)
            {
                HeroSkillSet entry = group.Sets[i];
                if (entry == null)
                {
                    errors.Add($"{prefix}: entry {i} is null.");
                    continue;
                }

                if (entry.Level < 0 || entry.Level > maxLevel)
                    errors.Add($"{prefix}: entry {i} Level {entry.Level} is outside 0-{maxLevel}.");
                if (entry.Level < previousLevel)
                    errors.Add($"{prefix}: entries are not ordered by Level at index {i}.");
                previousLevel = entry.Level;

                if (entry.Skill == null)
                {
                    errors.Add($"{prefix}: entry {i} has no Skill.");
                    continue;
                }

                if (entry.Skill is ActiveSkillData activeSkill)
                {
                    if (!activeUnlockLevels.TryAdd(activeSkill, entry.Level))
                        errors.Add($"{prefix}: active skill '{activeSkill.name}' is registered more than once.");

                    bool isBasicAttack = activeSkill.Priority == 0;
                    if (isBasicAttack)
                    {
                        basicAttackCount++;
                        if (entry.Level != 0
                            || activeSkill.Trigger is not NoneTriggerData
                            || !Mathf.Approximately(activeSkill.ActivationChance, 1f))
                        {
                            errors.Add(
                                $"{prefix}: basic attack '{activeSkill.name}' must be Level 0, " +
                                "Priority 0, NoneTrigger, and ActivationChance 1.");
                        }
                    }
                    else if (activeSkill.Priority < 1)
                    {
                        errors.Add($"{prefix}: active skill '{activeSkill.name}' must have Priority above 0.");
                    }

                    if (entry.Level == 0 && !isBasicAttack)
                        errors.Add($"{prefix}: Level 0 may only contain the basic attack.");

                    ValidateActiveSkill(activeSkill, prefix, errors, out EffectGraph graph);
                    if (!activeGraphs.ContainsKey(activeSkill))
                        activeGraphs.Add(activeSkill, graph);
                }
                else if (entry.Level == 0)
                {
                    errors.Add($"{prefix}: Level 0 may only contain the basic attack.");
                }
                else if (entry.Skill is PassiveSkillData passiveSkill)
                {
                    ValidatePassive(passiveSkill, prefix, errors);
                }
            }

            if (basicAttackCount != 1)
                errors.Add($"{prefix}: exactly one basic attack is required. Count: {basicAttackCount}.");

            ValidateEnhancements(
                group.Sets,
                activeUnlockLevels,
                activeGraphs,
                prefix,
                errors);
        }

        private static void ValidateActiveSkill(
            ActiveSkillData skill,
            string prefix,
            List<string> errors,
            out EffectGraph graph)
        {
            graph = new EffectGraph();

            if (skill.Execution == null)
                errors.Add($"{prefix}: active skill '{skill.name}' has no Execution.");
            if (skill.Finder == null)
                errors.Add($"{prefix}: active skill '{skill.name}' has no Finder.");
            if (skill.Trigger == null)
                errors.Add($"{prefix}: active skill '{skill.name}' has no Trigger.");
            if (skill.AnimationData == null)
                errors.Add($"{prefix}: active skill '{skill.name}' has no AnimationData.");

            ValidateFinite(skill.ChargeTime, 0f, float.MaxValue, $"{skill.name}.ChargeTime", errors);
            ValidateFinite(skill.ActivationChance, 0f, 1f, $"{skill.name}.ActivationChance", errors);

            if (skill.AnimationData != null)
            {
                ValidateFinite(
                    skill.AnimationData.ReadyMotionTime,
                    0f,
                    float.MaxValue,
                    $"{skill.name}.ReadyMotionTime",
                    errors);
                ValidateFinite(
                    skill.AnimationData.ExecutionMotionTime,
                    0f,
                    float.MaxValue,
                    $"{skill.name}.ExecutionMotionTime",
                    errors);
            }

            if (skill.Execution != null)
                WalkContainer(skill.Execution, graph, new HashSet<ScriptableObject>(), prefix, errors);
        }

        private static void ValidatePassive(
            PassiveSkillData passive,
            string prefix,
            List<string> errors)
        {
            if (passive.Target == null)
                errors.Add($"{prefix}: passive '{passive.name}' has no Target.");
            if (passive.Effects == null || passive.Effects.Count == 0)
            {
                errors.Add($"{prefix}: passive '{passive.name}' has no Effects.");
                return;
            }

            for (int i = 0; i < passive.Effects.Count; i++)
            {
                if (passive.Effects[i] == null)
                    errors.Add($"{prefix}: passive '{passive.name}' has a null Effect at index {i}.");
            }
        }

        private static void ValidateEnhancements(
            IReadOnlyList<HeroSkillSet> entries,
            IReadOnlyDictionary<ActiveSkillData, int> unlockLevels,
            IReadOnlyDictionary<ActiveSkillData, EffectGraph> graphs,
            string prefix,
            List<string> errors)
        {
            Dictionary<ActiveSkillData, float> chanceTotals = new();
            Dictionary<ActiveSkillData, float> reductionTotals = new();

            foreach (ActiveSkillData activeSkill in unlockLevels.Keys)
                chanceTotals[activeSkill] = activeSkill.ActivationChance;

            for (int i = 0; i < entries.Count; i++)
            {
                HeroSkillSet entry = entries[i];
                if (entry?.Skill is not ExtraEffectData extraEffect)
                    continue;

                ActiveSkillData target = extraEffect.TargetSkill;
                if (target == null)
                {
                    errors.Add($"{prefix}: enhancer '{extraEffect.name}' has no target active skill.");
                    continue;
                }
                if (!unlockLevels.TryGetValue(target, out int unlockLevel) || unlockLevel > entry.Level)
                {
                    errors.Add(
                        $"{prefix}: enhancer '{extraEffect.name}' targets an active skill " +
                        "that is not unlocked at the same or an earlier Level.");
                    continue;
                }

                ValidateExtraEffect(extraEffect, graphs[target], prefix, errors);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                HeroSkillSet entry = entries[i];
                if (entry?.Skill == null
                    || entry.Skill is ActiveSkillData
                    || entry.Skill is PassiveSkillData
                    || entry.Skill is ExtraEffectData)
                    continue;

                ActiveSkillData target = GetEnhancementTarget(entry.Skill);
                if (target == null)
                {
                    errors.Add($"{prefix}: enhancer '{entry.Skill.name}' has no target active skill.");
                    continue;
                }

                if (!unlockLevels.TryGetValue(target, out int unlockLevel) || unlockLevel > entry.Level)
                {
                    errors.Add(
                        $"{prefix}: enhancer '{entry.Skill.name}' targets an active skill " +
                        "that is not unlocked at the same or an earlier Level.");
                    continue;
                }

                EffectGraph graph = graphs[target];
                switch (entry.Skill)
                {
                    case EffectValueEnhanceData valueEnhancer:
                        ValidateValueEnhancer(valueEnhancer, graph, prefix, errors);
                        break;

                    case ActivationChanceEnhanceData chanceEnhancer:
                        ValidateFinite(
                            chanceEnhancer.AddChance,
                            0f,
                            1f,
                            $"{chanceEnhancer.name}.AddChance",
                            errors);
                        chanceTotals[target] += chanceEnhancer.AddChance;
                        if (chanceTotals[target] > 1f + Mathf.Epsilon)
                            errors.Add($"{prefix}: activation chance enhancements for '{target.name}' exceed 1.");
                        break;

                    case TriggerRequirementReductionData reduction:
                        if (target.Trigger is not ManaTriggerData and not HitCountTriggerData)
                            errors.Add($"{prefix}: trigger reduction '{reduction.name}' targets an unsupported Trigger.");
                        ValidateFinite(
                            reduction.ReductionRatio,
                            0f,
                            1f,
                            $"{reduction.name}.ReductionRatio",
                            errors);
                        reductionTotals.TryGetValue(target, out float total);
                        total += reduction.ReductionRatio;
                        reductionTotals[target] = total;
                        if (total > 1f + Mathf.Epsilon)
                            errors.Add($"{prefix}: trigger reductions for '{target.name}' exceed 1.");
                        break;

                    default:
                        errors.Add($"{prefix}: unsupported skill data type '{entry.Skill.GetType().Name}'.");
                        break;
                }
            }
        }

        private static ActiveSkillData GetEnhancementTarget(SkillBaseData skill)
        {
            return skill switch
            {
                Enhancer enhancer => enhancer.TargetSkill,
                ActivationChanceEnhanceData enhancer => enhancer.TargetSkill,
                TriggerRequirementReductionData enhancer => enhancer.TargetSkill,
                ExtraEffectData enhancer => enhancer.TargetSkill,
                _ => null
            };
        }

        private static void ValidateValueEnhancer(
            EffectValueEnhanceData enhancer,
            EffectGraph graph,
            string prefix,
            List<string> errors)
        {
            if (enhancer.TargetEffect == null || !graph.Effects.Contains(enhancer.TargetEffect))
            {
                errors.Add($"{prefix}: value enhancer '{enhancer.name}' targets an unknown Effect.");
                return;
            }

            IReadOnlyList<EffectStatDefinition> stats = enhancer.TargetEffect.GetEnhanceableStats();
            bool validKey = false;
            if (!string.IsNullOrWhiteSpace(enhancer.TargetStatKey) && stats != null)
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    if (stats[i].Key == enhancer.TargetStatKey)
                    {
                        validKey = true;
                        break;
                    }
                }
            }

            if (!validKey)
                errors.Add($"{prefix}: value enhancer '{enhancer.name}' has an invalid stat key.");
            if (float.IsNaN(enhancer.AddValue) || float.IsInfinity(enhancer.AddValue))
                errors.Add($"{prefix}: value enhancer '{enhancer.name}' AddValue is not finite.");
        }

        private static void ValidateExtraEffect(
            ExtraEffectData enhancer,
            EffectGraph graph,
            string prefix,
            List<string> errors)
        {
            if (enhancer.EffectContainer == null
                || !graph.Containers.Contains(enhancer.EffectContainer))
            {
                errors.Add($"{prefix}: extra effect '{enhancer.name}' targets an unknown container.");
            }

            if (enhancer.Effect == null)
            {
                errors.Add($"{prefix}: extra effect '{enhancer.name}' has no Effect.");
                return;
            }

            WalkEffect(
                enhancer.Effect,
                graph,
                new HashSet<ScriptableObject>(),
                prefix,
                errors);
        }

        private static void WalkContainer(
            ScriptableObject container,
            EffectGraph graph,
            HashSet<ScriptableObject> visiting,
            string prefix,
            List<string> errors)
        {
            if (container == null)
                return;
            if (!visiting.Add(container))
            {
                errors.Add($"{prefix}: circular Effect container reference at '{container.name}'.");
                return;
            }
            if (!graph.Containers.Add(container))
            {
                errors.Add($"{prefix}: Effect container '{container.name}' is referenced more than once.");
                visiting.Remove(container);
                return;
            }

            if (container is not IEffectContainer effectContainer)
            {
                errors.Add($"{prefix}: '{container.name}' is not an Effect container.");
                visiting.Remove(container);
                return;
            }

            List<EffectBase> effects;
            try
            {
                effects = effectContainer.GetEffects;
            }
            catch (Exception exception)
            {
                errors.Add($"{prefix}: failed to read Effects from '{container.name}': {exception.Message}");
                visiting.Remove(container);
                return;
            }

            if (effects == null)
            {
                errors.Add($"{prefix}: Effect list in '{container.name}' is null.");
                visiting.Remove(container);
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                EffectBase effect = effects[i];
                if (effect == null)
                {
                    errors.Add($"{prefix}: '{container.name}' has a null Effect at index {i}.");
                    continue;
                }
                WalkEffect(effect, graph, visiting, prefix, errors);
            }

            visiting.Remove(container);
        }

        private static void WalkEffect(
            EffectBase effect,
            EffectGraph graph,
            HashSet<ScriptableObject> visiting,
            string prefix,
            List<string> errors)
        {
            if (!graph.Effects.Add(effect))
            {
                errors.Add($"{prefix}: Effect '{effect.name}' is referenced more than once.");
                return;
            }

            ValidateFinite(effect.Chance, 0f, 1f, $"{effect.name}.Chance", errors);

            if (effect is IEffectContainer)
                WalkContainer(effect, graph, visiting, prefix, errors);

            if (effect is ProjectileSpawnEffect projectileEffect)
            {
                if (projectileEffect.Projectile == null)
                    errors.Add($"{prefix}: projectile effect '{effect.name}' has no Projectile data.");
                else
                    WalkContainer(projectileEffect.Projectile, graph, visiting, prefix, errors);
            }
            else if (effect is SummonSpawnEffect summonEffect)
            {
                if (summonEffect.Move == null)
                    errors.Add($"{prefix}: summon effect '{effect.name}' has no Move data.");
                if (summonEffect.Execution == null)
                    errors.Add($"{prefix}: summon effect '{effect.name}' has no Execution data.");
                else
                    WalkContainer(summonEffect.Execution, graph, visiting, prefix, errors);
            }
        }

        private static void ValidateFinite(
            float value,
            float min,
            float max,
            string name,
            List<string> errors)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max)
                errors.Add($"{name} must be a finite value between {min} and {max}.");
        }
    }
}
