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

        public SkillSet CreateSkill(Hero owner, int level, SkillSetContainer set)
        {
            List<HeroSkillSet> sets = set.GetSets(level);
            ValidateBasicAttack(sets, set.name);

            SkillSet returnSkillSet = new SkillSet();

            Dictionary<ActiveSkillData, RuntimeExecution> runtimeExecutions = new();
            Dictionary<ActiveSkillData, ActiveSkill> runtimeActiveSkills = new();

            Queue<EffectValueEnhanceData> statEnhancerDatas = new Queue<EffectValueEnhanceData>();
            Queue<ActivationChanceEnhanceData> activationChanceEnhancers = new Queue<ActivationChanceEnhanceData>();
            Queue<TriggerRequirementReductionData> triggerRequirementReductions = new Queue<TriggerRequirementReductionData>();
            Queue<ExtraEffectData> extraEffects = new Queue<ExtraEffectData>();

            foreach (var data in sets)
            {
                if (data.Skill == null)
                {
                    throw new InvalidOperationException(
                        $"SkillSet '{set.name}' has null or missing skill at level {data.Level}.");
                }

                var Skill = data.Skill;

                switch (Skill)
                {
                    case ActiveSkillData activeSkill:

                        RuntimeExecution runtimeSkillEffect = new RuntimeExecution(activeSkill.Execution);
    
                        runtimeExecutions.Add(activeSkill, runtimeSkillEffect);

                        ActiveSkill skill = CreateActiveSkill(activeSkill, owner, runtimeSkillEffect);
                        runtimeActiveSkills.Add(activeSkill, skill);
                        returnSkillSet.ActiveSkills.Add(skill);

                        break;
                    case PassiveSkillData passiveSkill:

                        PassiveSkill passive = CreatePassiveSkill(passiveSkill, owner);
                        passive.SetUID(passiveSkill.UID);

                        returnSkillSet.PassiveSkills.Add(passive);

                        break;
                    case EffectValueEnhanceData enhanceValueData:
                        statEnhancerDatas.Enqueue(enhanceValueData);
                        break;

                    case ActivationChanceEnhanceData activationChanceEnhanceData:
                        activationChanceEnhancers.Enqueue(activationChanceEnhanceData);
                        break;

                    case TriggerRequirementReductionData triggerRequirementReductionData:
                        triggerRequirementReductions.Enqueue(triggerRequirementReductionData);
                        break;

                    case ExtraEffectData extraEffectData:
                        extraEffects.Enqueue(extraEffectData);
                        break;

                    default:
                        throw new InvalidOperationException($"Not Definition Skill Type : {Skill.name}");
                }
            }

            //스킬 추가에 대한 처리가 가장먼저 되어야 함

            SkillEnhancementApplier.ApplyExtraEffect(runtimeExecutions, extraEffects);
            SkillEnhancementApplier.ApplyStatEnhance(runtimeExecutions, statEnhancerDatas);
            SkillEnhancementApplier.ApplyActivationChanceEnhance(runtimeActiveSkills, activationChanceEnhancers);
            SkillEnhancementApplier.ApplyTriggerRequirementReduction(runtimeActiveSkills, triggerRequirementReductions);

            return returnSkillSet;
        }
        public ActiveSkill CreateActiveSkill(ActiveSkillData skillSO, Hero owner, RuntimeExecution runtimeExecution)
        {
            ITrigger trigger = TriggerFactory.CreateTrigger(skillSO.Trigger);
            IFinder target = FinderFactory.CreateTarget(skillSO.Finder, owner, _runtimeContext);

            SkillExecutionContext executionContext = new SkillExecutionContext(
                owner,
                skillSO.AnimationData,
                runtimeExecution.RuntimeExecutionData,
                skillSO.UID,
                skillSO.ChargeTime,
                runtimeExecution,
                target);

            IExecute execution = ExecutionFactory.CreateExecution(executionContext, _runtimeContext);

            ActiveSkill activeSkill = new ActiveSkill(
                skillSO.UID,
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

                basicAttack = activeSkill;
            }

            if (basicAttack == null)
            {
                throw new InvalidOperationException(
                    $"Skill set '{setName}' has no basic attack.");
            }
        }

        private PassiveSkill CreatePassiveSkill(PassiveSkillData passiveSkillSO, Hero owner)
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
    }
}
