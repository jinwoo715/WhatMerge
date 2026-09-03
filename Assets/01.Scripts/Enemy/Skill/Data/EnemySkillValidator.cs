using System;
using System.Collections.Generic;
using Skill.Data;

namespace WhatMerge.Enemies.Skills.Data
{
    public static class EnemySkillValidator
    {
        public static IReadOnlyList<string> Validate(EnemySkillCatalog catalog)
        {
            List<string> errors = new List<string>();
            ValidateCatalog(catalog, null, false, errors);
            return errors;
        }

        public static IReadOnlyList<string> Validate(
            EnemySkillCatalog catalog,
            IEnumerable<EnemyData> enemyData)
        {
            List<string> errors = new List<string>();
            List<EnemyData> enemies = new List<EnemyData>();
            HashSet<int> enemyUIDs = new HashSet<int>();

            if (enemyData == null)
            {
                errors.Add("EnemyData collection is null.");
            }
            else
            {
                int index = 0;
                foreach (EnemyData enemy in enemyData)
                {
                    if (enemy == null)
                    {
                        errors.Add($"EnemyData at index {index} is null.");
                    }
                    else
                    {
                        enemies.Add(enemy);
                        if (!enemyUIDs.Add(enemy.UID))
                            errors.Add($"EnemyData UID {enemy.UID} is duplicated.");
                    }

                    index++;
                }
            }

            HashSet<int> skillSetUIDs = ValidateCatalog(
                catalog,
                enemyUIDs,
                enemyData != null,
                errors);

            ValidateEnemySkillSetReferences(enemies, skillSetUIDs, errors);
            return errors;
        }

        public static void ValidateOrThrow(EnemySkillCatalog catalog)
        {
            ThrowIfInvalid(catalog, Validate(catalog));
        }

        public static void ValidateOrThrow(
            EnemySkillCatalog catalog,
            IEnumerable<EnemyData> enemyData)
        {
            ThrowIfInvalid(catalog, Validate(catalog, enemyData));
        }

        private static HashSet<int> ValidateCatalog(
            EnemySkillCatalog catalog,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            HashSet<int> skillSetUIDs = new HashSet<int>();
            if (catalog == null)
            {
                errors.Add("EnemySkillCatalog is null.");
                return skillSetUIDs;
            }

            if (catalog.SkillSets == null || catalog.SkillSets.Count == 0)
            {
                errors.Add($"EnemySkillCatalog '{catalog.name}' has no skill sets.");
                return skillSetUIDs;
            }

            for (int i = 0; i < catalog.SkillSets.Count; i++)
            {
                EnemySkillSetContainer skillSet = catalog.SkillSets[i];
                if (skillSet == null)
                {
                    errors.Add($"EnemySkillCatalog '{catalog.name}' has a null skill set at index {i}.");
                    continue;
                }

                if (skillSet.UID <= 0)
                    errors.Add($"Enemy skill set '{skillSet.name}' UID must be greater than zero.");
                else if (!skillSetUIDs.Add(skillSet.UID))
                    errors.Add($"Enemy skill set UID {skillSet.UID} is duplicated.");

                ValidateSkillSet(skillSet, enemyUIDs, validateEnemyReferences, errors);
            }

            return skillSetUIDs;
        }

        private static void ValidateSkillSet(
            EnemySkillSetContainer skillSet,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            string path = $"SkillSet[{skillSet.UID}]";
            if (skillSet.Skills == null || skillSet.Skills.Count == 0)
            {
                errors.Add($"{path} has no skills.");
                return;
            }

            HashSet<EnemySkillData> skills = new HashSet<EnemySkillData>();
            for (int i = 0; i < skillSet.Skills.Count; i++)
            {
                EnemySkillData skill = skillSet.Skills[i];
                if (skill == null)
                {
                    errors.Add($"{path} has a null skill at index {i}.");
                    continue;
                }

                if (!skills.Add(skill))
                {
                    errors.Add($"{path} registers skill '{skill.name}' more than once.");
                    continue;
                }

                ValidateSkill(
                    skill,
                    $"{path}/Skill[{i}]",
                    enemyUIDs,
                    validateEnemyReferences,
                    errors);
            }
        }

        private static void ValidateSkill(
            EnemySkillData skill,
            string path,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(skill.Name))
                errors.Add($"{path} '{skill.name}' has an empty Name.");

            if (skill.ExecutionPolicy == null)
                errors.Add($"{path} '{skill.name}' has no execution policy.");
            else
                ValidateExecutionPolicy(skill.ExecutionPolicy, path, errors);

            if (skill.Trigger == null)
                errors.Add($"{path} '{skill.name}' has no Trigger.");
            else
            {
                ValidateTrigger(
                    skill.Trigger,
                    skill.ExecutionPolicy,
                    path,
                    enemyUIDs,
                    validateEnemyReferences,
                    errors);
            }

            if (skill.Actions == null || skill.Actions.Count == 0)
            {
                errors.Add($"{path} '{skill.name}' has no Actions.");
                return;
            }

            for (int i = 0; i < skill.Actions.Count; i++)
            {
                EnemySkillActionData action = skill.Actions[i];
                string actionPath = $"{path}/Action[{i}]";
                if (action == null)
                {
                    errors.Add($"{actionPath} is null.");
                    continue;
                }

                ValidateAction(
                    action,
                    skill.Trigger,
                    actionPath,
                    enemyUIDs,
                    validateEnemyReferences,
                    errors);
            }
        }

        private static void ValidateExecutionPolicy(
            EnemySkillExecutionPolicy policy,
            string path,
            List<string> errors)
        {
            if (policy.Priority < 0)
                errors.Add($"{path} Priority must be zero or greater.");
            ValidateFiniteNonNegative(policy.Cooldown, $"{path} Cooldown", errors);
            if (policy.MaxActivationCount < 0)
                errors.Add($"{path} MaxActivationCount must be zero or greater.");
        }

        private static void ValidateTrigger(
            EnemySkillTriggerData trigger,
            EnemySkillExecutionPolicy policy,
            string path,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            switch (trigger)
            {
                case EnemyTimeTriggerData timeTrigger:
                    ValidateFiniteNonNegative(timeTrigger.InitialDelay, $"{path} InitialDelay", errors);
                    ValidateFiniteNonNegative(timeTrigger.Interval, $"{path} Interval", errors);
                    if (policy != null
                        && policy.MaxActivationCount != 1
                        && timeTrigger.Interval <= 0f)
                    {
                        errors.Add($"{path} repeating Time Trigger requires Interval greater than zero.");
                    }
                    break;

                case EnemyHitCountTriggerData hitCountTrigger:
                    if (hitCountTrigger.RequiredHitCount < 1)
                        errors.Add($"{path} RequiredHitCount must be at least one.");
                    break;

                case EnemyHpRatioTriggerData hpRatioTrigger:
                    ValidateFiniteRange(
                        hpRatioTrigger.ThresholdRatio,
                        0f,
                        1f,
                        $"{path} ThresholdRatio",
                        errors);
                    break;

                case EnemyDeathTriggerData:
                    if (policy != null && policy.MaxActivationCount != 1)
                        errors.Add($"{path} Death Trigger requires MaxActivationCount of one.");
                    break;

                case EnemyProximityTriggerData proximityTrigger:
                    if (proximityTrigger.TargetEnemyUID <= 0)
                    {
                        errors.Add($"{path} TargetEnemyUID must be greater than zero.");
                    }
                    else if (validateEnemyReferences && !enemyUIDs.Contains(proximityTrigger.TargetEnemyUID))
                    {
                        errors.Add(
                            $"{path} references missing target EnemyData UID " +
                            $"{proximityTrigger.TargetEnemyUID}.");
                    }

                    ValidateFinitePositive(
                        proximityTrigger.DetectionDistance,
                        $"{path} DetectionDistance",
                        errors);
                    if (policy != null && policy.MaxActivationCount != 1)
                        errors.Add($"{path} Enemy Proximity Trigger requires MaxActivationCount of one.");
                    break;

                default:
                    errors.Add($"{path} uses unsupported Trigger type '{trigger.GetType().Name}'.");
                    break;
            }
        }

        private static void ValidateAction(
            EnemySkillActionData action,
            EnemySkillTriggerData trigger,
            string path,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            if (action.Target != null)
                ValidateTarget(action.Target, trigger, path, errors);

            if (action.Effects == null || action.Effects.Count == 0)
            {
                errors.Add($"{path} has no Effects.");
                return;
            }

            for (int i = 0; i < action.Effects.Count; i++)
            {
                EnemySkillEffectData effect = action.Effects[i];
                string effectPath = $"{path}/Effect[{i}]";
                if (effect == null)
                {
                    errors.Add($"{effectPath} is null.");
                    continue;
                }

                ValidateEffect(effect, effectPath, enemyUIDs, validateEnemyReferences, errors);
                ValidateTargetCompatibility(action.Target, effect, effectPath, errors);
            }
        }

        private static void ValidateTarget(
            EnemySkillTargetData target,
            EnemySkillTriggerData trigger,
            string path,
            List<string> errors)
        {
            switch (target)
            {
                case SelfEnemyTargetData:
                case AllHeroFromEnemyTargetData:
                    break;

                case TriggeredEnemyTargetData:
                    if (!(trigger is EnemyProximityTriggerData))
                        errors.Add($"{path} Triggered Enemy Target requires an Enemy Proximity Trigger.");
                    break;

                case NearAllyEnemyTargetData nearEnemy:
                    ValidateFinitePositive(nearEnemy.Radius, $"{path} Target Radius", errors);
                    ValidateAllowedTypes(nearEnemy.AllowedTypes, path, errors);
                    break;

                case AllAllyEnemyTargetData allEnemies:
                    ValidateAllowedTypes(allEnemies.AllowedTypes, path, errors);
                    break;

                case NearHeroFromEnemyTargetData nearHero:
                    ValidateFinitePositive(nearHero.Radius, $"{path} Target Radius", errors);
                    break;

                default:
                    errors.Add($"{path} uses unsupported Target type '{target.GetType().Name}'.");
                    break;
            }
        }

        private static void ValidateEffect(
            EnemySkillEffectData effect,
            string path,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            ValidateFiniteRange(effect.Chance, 0f, 1f, $"{path} Chance", errors);

            switch (effect)
            {
                case EnemyBuffEffectData enemyBuff:
                    ValidateEnemyBuff(enemyBuff, path, errors);
                    break;

                case HeroDebuffEffectData heroDebuff:
                    ValidateHeroDebuff(heroDebuff, path, errors);
                    break;

                case DispelHeroBuffEffectData dispel:
                    if (dispel.MaxDispelCount < 0)
                        errors.Add($"{path} MaxDispelCount must be zero or greater.");
                    ValidateRemovalPolicy(dispel.Policy, path, errors);
                    break;

                case CleanseEnemyDebuffEffectData cleanse:
                    if (cleanse.MaxCleanseCount < 0)
                        errors.Add($"{path} MaxCleanseCount must be zero or greater.");
                    ValidateRemovalPolicy(cleanse.Policy, path, errors);
                    break;

                case SpawnEnemyEffectData spawn:
                    ValidateSpawnEffect(spawn, path, enemyUIDs, validateEnemyReferences, errors);
                    break;

                case MergeEnemyEffectData merge:
                    ValidateMergeEffect(merge, path, enemyUIDs, validateEnemyReferences, errors);
                    break;

                case EnemySkillVFXEffectData vfxEffect:
                    if (vfxEffect.VFX == null)
                        errors.Add($"{path} VFX Effect has no VFXData.");
                    break;

                default:
                    errors.Add($"{path} uses unsupported Effect type '{effect.GetType().Name}'.");
                    break;
            }
        }

        private static void ValidateEnemyBuff(
            EnemyBuffEffectData effect,
            string path,
            List<string> errors)
        {
            ValidateFiniteNonNegative(effect.Duration, $"{path} Duration", errors);
            if (effect.Buffs == null || effect.Buffs.Count == 0)
            {
                errors.Add($"{path} has no enemy Buff entries.");
                return;
            }

            HashSet<EnemyStatType> statTypes = new HashSet<EnemyStatType>();
            for (int i = 0; i < effect.Buffs.Count; i++)
            {
                EnemyBuffStatData buff = effect.Buffs[i];
                string itemPath = $"{path}/Buff[{i}]";
                if (buff == null)
                {
                    errors.Add($"{itemPath} is null.");
                    continue;
                }

                if (!Enum.IsDefined(typeof(EnemyStatType), buff.StatType))
                    errors.Add($"{itemPath} has undefined StatType {(int)buff.StatType}.");
                else if (!statTypes.Add(buff.StatType))
                    errors.Add($"{itemPath} duplicates stat type {buff.StatType}.");
                ValidatePositiveModifier(
                    buff.FixedIncrease,
                    buff.MultiplierIncrease,
                    itemPath,
                    errors);
            }
        }

        private static void ValidateHeroDebuff(
            HeroDebuffEffectData effect,
            string path,
            List<string> errors)
        {
            ValidateFiniteNonNegative(effect.Duration, $"{path} Duration", errors);
            if (effect.Debuffs == null || effect.Debuffs.Count == 0)
            {
                errors.Add($"{path} has no hero Debuff entries.");
                return;
            }

            HashSet<WhatMerge.Heros.HeroStatType> statTypes =
                new HashSet<WhatMerge.Heros.HeroStatType>();
            for (int i = 0; i < effect.Debuffs.Count; i++)
            {
                HeroDebuffStatData debuff = effect.Debuffs[i];
                string itemPath = $"{path}/Debuff[{i}]";
                if (debuff == null)
                {
                    errors.Add($"{itemPath} is null.");
                    continue;
                }

                if (!Enum.IsDefined(typeof(WhatMerge.Heros.HeroStatType), debuff.StatType))
                    errors.Add($"{itemPath} has undefined StatType {(int)debuff.StatType}.");
                else if (!statTypes.Add(debuff.StatType))
                    errors.Add($"{itemPath} duplicates stat type {debuff.StatType}.");
                ValidatePositiveModifier(
                    debuff.FixedReduction,
                    debuff.MultiplierReduction,
                    itemPath,
                    errors);
            }
        }

        private static void ValidateSpawnEffect(
            SpawnEnemyEffectData effect,
            string path,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            if (effect.EnemyUID <= 0)
                errors.Add($"{path} EnemyUID must be greater than zero.");
            else if (validateEnemyReferences && !enemyUIDs.Contains(effect.EnemyUID))
                errors.Add($"{path} references missing EnemyData UID {effect.EnemyUID}.");

            if (effect.Count < 1)
                errors.Add($"{path} Count must be at least one.");
            ValidateFiniteNonNegative(effect.SpawnInterval, $"{path} SpawnInterval", errors);

            if (!Enum.IsDefined(typeof(EnemySpawnPositionType), effect.SpawnPositionType))
            {
                errors.Add(
                    $"{path} has undefined SpawnPositionType {(int)effect.SpawnPositionType}.");
                return;
            }

            if (effect.SpawnPositionType == EnemySpawnPositionType.AroundOwner)
                ValidateFinitePositive(effect.AroundOwnerRadius, $"{path} AroundOwnerRadius", errors);
            if (effect.SpawnPositionType == EnemySpawnPositionType.RelativeToOwnerPath
                && !IsFinite(effect.PathDistanceOffset))
            {
                errors.Add(
                    $"{path} PathDistanceOffset must be finite. " +
                    $"Current value: {effect.PathDistanceOffset}.");
            }
        }

        private static void ValidateMergeEffect(
            MergeEnemyEffectData effect,
            string path,
            HashSet<int> enemyUIDs,
            bool validateEnemyReferences,
            List<string> errors)
        {
            if (effect.ResultEnemyUID <= 0)
                errors.Add($"{path} ResultEnemyUID must be greater than zero.");
            else if (validateEnemyReferences && !enemyUIDs.Contains(effect.ResultEnemyUID))
                errors.Add($"{path} references missing result EnemyData UID {effect.ResultEnemyUID}.");

            if (effect.Chance != 1f)
                errors.Add($"{path} Merge Enemy Effect requires Chance of one.");
        }

        private static void ValidateRemovalPolicy(
            EnemyStatusRemovalPolicy policy,
            string path,
            List<string> errors)
        {
            if (!Enum.IsDefined(typeof(EnemyStatusRemovalPolicy), policy))
                errors.Add($"{path} has undefined removal Policy {(int)policy}.");
        }

        private static void ValidateTargetCompatibility(
            EnemySkillTargetData target,
            EnemySkillEffectData effect,
            string path,
            List<string> errors)
        {
            if (effect.RequiresTarget && target == null)
            {
                errors.Add($"{path} requires a Target.");
                return;
            }

            if (effect is MergeEnemyEffectData && !(target is TriggeredEnemyTargetData))
                errors.Add($"{path} Merge Enemy Effect requires a Triggered Enemy Target.");

            if (target != null && effect.TargetType != EnemySkillEffectTargetType.Any)
            {
                bool compatible = effect.TargetType == EnemySkillEffectTargetType.Enemy
                    ? target.Category == EnemySkillTargetCategory.Enemy
                    : target.Category == EnemySkillTargetCategory.Hero;

                if (!compatible)
                {
                    errors.Add(
                        $"{path} expects {effect.TargetType} targets, " +
                        $"but Action target '{target.name}' selects {target.Category}.");
                }
            }

            if (target == null
                && effect.VFX != null
                && (effect.VFX.PositionType == VFXSpawnPositionTpye.Target
                    || effect.VFX.PositionType == VFXSpawnPositionTpye.Middle))
            {
                errors.Add($"{path} VFX position {effect.VFX.PositionType} requires a Target.");
            }
        }

        private static void ValidateAllowedTypes(
            List<EnemyType> allowedTypes,
            string path,
            List<string> errors)
        {
            if (allowedTypes == null)
            {
                errors.Add($"{path} AllowedTypes is null. Use an empty list to allow every EnemyType.");
                return;
            }

            HashSet<EnemyType> types = new HashSet<EnemyType>();
            for (int i = 0; i < allowedTypes.Count; i++)
            {
                EnemyType type = allowedTypes[i];
                if (!Enum.IsDefined(typeof(EnemyType), type))
                    errors.Add($"{path} AllowedTypes contains undefined value {(int)type}.");
                else if (!types.Add(type))
                    errors.Add($"{path} AllowedTypes contains duplicate value {type}.");
            }
        }

        private static void ValidateEnemySkillSetReferences(
            List<EnemyData> enemies,
            HashSet<int> skillSetUIDs,
            List<string> errors)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData enemy = enemies[i];
                if (enemy.EnemyType == EnemyType.Special)
                {
                    if (enemy.SkillSetUID <= 0)
                    {
                        errors.Add($"Special EnemyData UID {enemy.UID} requires SkillSetUID greater than zero.");
                    }
                    else if (!skillSetUIDs.Contains(enemy.SkillSetUID))
                    {
                        errors.Add(
                            $"Special EnemyData UID {enemy.UID} references missing " +
                            $"EnemySkillSet UID {enemy.SkillSetUID}.");
                    }
                }
                else if (enemy.SkillSetUID != 0)
                {
                    errors.Add(
                        $"EnemyData UID {enemy.UID} is {enemy.EnemyType} and must use SkillSetUID zero.");
                }
            }
        }

        private static void ValidatePositiveModifier(
            float fixedValue,
            float multiplier,
            string path,
            List<string> errors)
        {
            ValidateFiniteNonNegative(fixedValue, $"{path} fixed value", errors);
            ValidateFiniteNonNegative(multiplier, $"{path} multiplier", errors);
            if (IsFinite(fixedValue)
                && IsFinite(multiplier)
                && fixedValue <= 0f
                && multiplier <= 0f)
            {
                errors.Add($"{path} must define a positive fixed value or multiplier.");
            }
        }

        private static void ValidateFinitePositive(
            float value,
            string path,
            List<string> errors)
        {
            if (!IsFinite(value) || value <= 0f)
                errors.Add($"{path} must be finite and greater than zero. Current value: {value}.");
        }

        private static void ValidateFiniteNonNegative(
            float value,
            string path,
            List<string> errors)
        {
            if (!IsFinite(value) || value < 0f)
                errors.Add($"{path} must be finite and zero or greater. Current value: {value}.");
        }

        private static void ValidateFiniteRange(
            float value,
            float min,
            float max,
            string path,
            List<string> errors)
        {
            if (!IsFinite(value) || value < min || value > max)
                errors.Add($"{path} must be finite and between {min} and {max}. Current value: {value}.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void ThrowIfInvalid(
            EnemySkillCatalog catalog,
            IReadOnlyList<string> errors)
        {
            if (errors.Count == 0)
                return;

            throw new InvalidOperationException(
                $"Enemy skill validation failed for '{catalog?.name ?? "null"}':\n- " +
                string.Join("\n- ", errors));
        }
    }
}
