using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Heros;

namespace Skill
{
    public class SkillSet
    {
        public List<IActiveSkill> ActiveSkills = new List<IActiveSkill>();
        public List<IPassiveSkill> PassiveSkills = new List<IPassiveSkill>();
    }
    public class SkillFactory
    {
        private SkillRuntimeContext _runtimeContext;

        public void Init(SkillRuntimeContext runtimeContext)
        {
            _runtimeContext = runtimeContext;
        }

        public SkillSet CreateSkill(
            Hero owner,
            HeroGrade grade,
            int level,
            SkillSetContainer set)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            List<HeroSkillSet> sets = set.GetSets(grade, level);
            ValidateBasicAttack(sets, set.name);

            SkillSet result = new SkillSet();
            Dictionary<ActiveSkillData, RuntimeExecution> runtimeExecutions = new();
            Dictionary<ActiveSkillData, ActiveSkill> runtimeActiveSkills = new();
            Queue<EffectValueEnhanceData> statEnhancers = new();
            Queue<ActivationChanceEnhanceData> activationChanceEnhancers = new();
            Queue<SequenceCountEnhanceData> sequenceCountEnhancers = new();
            Queue<TriggerRequirementReductionData> triggerRequirementReductions = new();
            Queue<ExtraEffectData> extraEffects = new();
            List<PassiveGoldSkillData> goldPassiveData = new(3);

            try
            {
                foreach (HeroSkillSet entry in sets)
                {
                    if (entry.Skill == null)
                    {
                        throw new InvalidOperationException(
                            $"SkillSet '{set.name}' has null or missing skill at level {entry.Level}.");
                    }

                    switch (entry.Skill)
                    {
                        case ActiveSkillData activeSkillData:
                            RuntimeExecution runtimeExecution = new RuntimeExecution(activeSkillData.Execution);

                            try
                            {
                                runtimeExecutions.Add(activeSkillData, runtimeExecution);
                                ActiveSkill activeSkill = CreateActiveSkill(
                                    activeSkillData,
                                    owner,
                                    runtimeExecution);
                                runtimeActiveSkills.Add(activeSkillData, activeSkill);
                                result.ActiveSkills.Add(activeSkill);
                            }
                            catch
                            {
                                TryDispose(runtimeExecution);
                                throw;
                            }

                            break;

                        case PassiveGoldSkillData gold:
                            goldPassiveData.Add(gold);
                            break;

                        case PassiveSkillData passiveSkillData:
                            result.PassiveSkills.Add(CreatePassiveSkill(passiveSkillData, owner));
                            break;

                        case EffectValueEnhanceData effectValueEnhanceData:
                            statEnhancers.Enqueue(effectValueEnhanceData);
                            break;

                        case ActivationChanceEnhanceData activationChanceEnhanceData:
                            activationChanceEnhancers.Enqueue(activationChanceEnhanceData);
                            break;

                        case SequenceCountEnhanceData sequenceCountEnhanceData:
                            sequenceCountEnhancers.Enqueue(sequenceCountEnhanceData);
                            break;

                        case TriggerRequirementReductionData triggerRequirementReductionData:
                            triggerRequirementReductions.Enqueue(triggerRequirementReductionData);
                            break;

                        case ExtraEffectData extraEffectData:
                            extraEffects.Enqueue(extraEffectData);
                            break;

                        default:
                            throw new InvalidOperationException(
                                $"Undefined skill data type: {entry.Skill.name}");
                    }
                }

                AddPeriodicGoldPassive(result.PassiveSkills, goldPassiveData);
                SkillEnhancementApplier.ApplyExtraEffect(runtimeExecutions, extraEffects);
                SkillEnhancementApplier.ApplyStatEnhance(runtimeExecutions, statEnhancers);
                SkillEnhancementApplier.ApplyActivationChanceEnhance(runtimeActiveSkills, activationChanceEnhancers);
                SkillEnhancementApplier.ApplySequenceCountEnhance(runtimeActiveSkills, sequenceCountEnhancers);
                SkillEnhancementApplier.ApplyTriggerRequirementReduction(runtimeActiveSkills, triggerRequirementReductions);

                return result;
            }
            catch
            {
                for (int i = 0; i < result.ActiveSkills.Count; i++)
                    TryDispose(result.ActiveSkills[i]);

                foreach (RuntimeExecution runtimeExecution in runtimeExecutions.Values)
                    TryDispose(runtimeExecution);

                throw;
            }
        }
        public ActiveSkill CreateActiveSkill(ActiveSkillData skillSO, Hero owner, RuntimeExecution runtimeExecution)
        {
            ITrigger trigger = TriggerFactory.CreateTrigger(skillSO.Trigger);
            IFinder target = FinderFactory.CreateTarget(skillSO.Finder, owner, _runtimeContext);

            SkillExecutionContext executionContext = new SkillExecutionContext(
                owner,
                skillSO.AnimationData,
                runtimeExecution.RuntimeExecutionData,
                skillSO.ChargeTime,
                runtimeExecution,
                target);

            IExecute execution = ExecutionFactory.CreateExecution(executionContext, _runtimeContext);

            ActiveSkill activeSkill = new ActiveSkill(
                owner,
                trigger,
                target,
                execution,
                skillSO.ActivationChance,
                skillSO.Priority);

            activeSkill.OnDispose += () => { runtimeExecution.Dispose(); };

            return activeSkill;
        }

        private static void ValidateBasicAttack(IReadOnlyList<HeroSkillSet> sets, string setName)
        {
            ActiveSkillData basicAttack = null;

            for (int i = 0; i < sets.Count; i++)
            {
                if (!(sets[i].Skill is ActiveSkillData activeSkill)
                    || activeSkill.Priority != 0)
                {
                    continue;
                }

                if (basicAttack != null)
                {
                    throw new InvalidOperationException(
                        $"Skill set '{setName}' has more than one basic attack: " +
                        $"'{basicAttack.name}', '{activeSkill.name}'.");
                }

                if (!(activeSkill.Trigger is NoneTriggerData))
                {
                    throw new InvalidOperationException(
                        $"Basic attack '{activeSkill.name}' must use a NoneTrigger.");
                }

                if (sets[i].Level != 0
                    || !Mathf.Approximately(activeSkill.ActivationChance, 1f))
                {
                    throw new InvalidOperationException(
                        $"Basic attack '{activeSkill.name}' must be Level 0 with activation chance 1.");
                }

                basicAttack = activeSkill;
            }

            if (basicAttack == null)
            {
                throw new InvalidOperationException(
                    $"Skill set '{setName}' has no basic attack.");
            }

            for (int i = 0; i < sets.Count; i++)
            {
                HeroSkillSet entry = sets[i];

                if (entry.Level == 0 && !ReferenceEquals(entry.Skill, basicAttack))
                {
                    throw new InvalidOperationException(
                        $"Skill set '{setName}' has a non-basic entry at Level 0.");
                }

                if (entry.Skill is ActiveSkillData activeSkill
                    && !ReferenceEquals(activeSkill, basicAttack)
                    && activeSkill.Priority <= 0)
                {
                    throw new InvalidOperationException(
                        $"Active skill '{activeSkill.name}' must have priority greater than zero.");
                }
            }
        }

        private PassiveSkill CreatePassiveSkill(PassiveSkillData passiveSkillSO, Hero owner)
        {
            return passiveSkillSO switch
            {
                PassiveBuffSkillData buff => CreateBuffPassiveSkill(buff, owner),
                PassiveDebuffSkillData debuff => CreateDebuffPassiveSkill(debuff, owner),
                _ => throw new InvalidOperationException(
                    $"Unsupported passive skill data: {passiveSkillSO.GetType().Name}")
            };
        }

        private PassiveSkill CreateBuffPassiveSkill(PassiveBuffSkillData passiveSkillSO, Hero owner)
        {
            var effects = passiveSkillSO.Effects;

            return passiveSkillSO.Target switch
            {
                SelfTargetData => new SelfBuffPassive(owner.StatModify, effects),
                NearHeroTargetData data => new NearHeroBuffPassive(
                    _runtimeContext.FieldHero,
                    owner,
                    effects,
                    data.TargetRange,
                    data.IncludeSelf),
                AllHeroTargetData => new AllHeroBuffPassive(_runtimeContext.FieldHero, owner, effects),
                _ => throw new InvalidOperationException($"Not Passive Target Exception {passiveSkillSO.Target}")
            };
        }

        private PassiveSkill CreateDebuffPassiveSkill(PassiveDebuffSkillData passiveSkillSO, Hero owner)
        {
            var effects = passiveSkillSO.Effects;

            return passiveSkillSO.Target switch
            {
                AllEnemyTargetData => new AllEnemyDebuffPassive(
                    _runtimeContext.FieldEnemy,
                    effects,
                    _runtimeContext.FatalStop,
                    passiveSkillSO.name),
                NearEnemyTargetData data => new NearEnemyDebuffPassive(
                    _runtimeContext.FieldEnemy,
                    owner,
                    effects,
                    data.Radius,
                    _runtimeContext.FatalStop,
                    passiveSkillSO.name),
                _ => throw new InvalidOperationException(
                    $"Unsupported passive debuff target: {passiveSkillSO.Target?.name ?? "null"}")
            };
        }

        private void AddPeriodicGoldPassive(
            ICollection<IPassiveSkill> passiveSkills,
            IReadOnlyList<PassiveGoldSkillData> dataList)
        {
            if (dataList.Count == 0)
                return;
            if (dataList.Count > 3)
            {
                throw new InvalidOperationException(
                    $"A hero grade can contain at most three gold passive entries. Count: {dataList.Count}.");
            }

            float intervalTime = dataList[0].IntervalTime;
            int totalGold = 0;

            for (int i = 0; i < dataList.Count; i++)
            {
                PassiveGoldSkillData data = dataList[i];

                if (float.IsNaN(data.IntervalTime)
                    || float.IsInfinity(data.IntervalTime)
                    || data.IntervalTime <= 0f)
                {
                    throw new InvalidOperationException(
                        $"Gold passive '{data.name}' IntervalTime must be positive and finite. " +
                        $"Current value: {data.IntervalTime}.");
                }

                if (!Mathf.Approximately(data.IntervalTime, intervalTime))
                {
                    throw new InvalidOperationException(
                        $"Gold passive entries must use the same IntervalTime. " +
                        $"Expected: {intervalTime}, '{data.name}': {data.IntervalTime}.");
                }

                if (data.GoldAmount <= 0)
                {
                    throw new InvalidOperationException(
                        $"Gold passive '{data.name}' GoldAmount must be greater than zero. " +
                        $"Current value: {data.GoldAmount}.");
                }

                try
                {
                    totalGold = checked(totalGold + data.GoldAmount);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidOperationException(
                        "The aggregated gold passive amount exceeds Int32.MaxValue.",
                        exception);
                }
            }

            passiveSkills.Add(new PeriodicGoldPassive(
                _runtimeContext.Gold,
                intervalTime,
                totalGold));
        }

        private static void TryDispose(IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }
        }
    }
}
