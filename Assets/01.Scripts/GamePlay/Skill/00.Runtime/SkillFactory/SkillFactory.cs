using Skill.Data;
using System.Collections;
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
            var sets = set.GetSets(level);

            SkillSet skillSet = new SkillSet();

            Dictionary<ActiveSkillData, RuntimeSkillEffect> activeSkillDatas = new();

            Queue<EffectValueEnhanceData> statEnhancerDatas = new Queue<EffectValueEnhanceData>();
            Queue<EffectChanceEnhanceData> chanceEnhancers = new Queue<EffectChanceEnhanceData>();
            Queue<ExtraEffectData> effects = new Queue<ExtraEffectData>();

            foreach (var data in sets)
            {
                var Skill = data.Skill;
                Debug.Log(Skill.name);

                switch (Skill.SkillType)
                {
                    case ESkillType.Active:

                        if (Skill is ActiveSkillData skillData)
                        {
                            ActiveSkillData so = skillData;

                            RuntimeSkillEffect runtimeSkillEffect = new RuntimeSkillEffect();
                            runtimeSkillEffect.SetEffect(so.Execution);

                            activeSkillDatas.Add(skillData, runtimeSkillEffect);

                            ActiveSkill skill = CreateActiveSkill(so, owner, runtimeSkillEffect.Effects);
                            skillSet.ActiveSkills.Add(skill);
                        }

                        break;
                    case ESkillType.Passive:

                        PassiveSkillData passiveSO = Skill as PassiveSkillData;
                        PassiveSkill passive = CreatePassiveSkill(passiveSO, owner);
                        passive.SetUID(passiveSO.UID);

                        skillSet.PassiveSkills.Add(passive);

                        break;

                    case ESkillType.SkillStatEnhancer:

                        EffectValueEnhanceData skillEnhancerData = Skill as EffectValueEnhanceData;
                        statEnhancerDatas.Enqueue(skillEnhancerData);

                        break;

                    case ESkillType.SkillChanceEnhancer:

                        EffectChanceEnhanceData skillChanceData = Skill as EffectChanceEnhanceData;
                        chanceEnhancers.Enqueue(skillChanceData);

                        break;

                    case ESkillType.ExtraEffect:

                        ExtraEffectData entry = (Skill as ExtraEffectData);
                        effects.Enqueue(entry);

                        break;
                }
            }

            ApplyExtraEffect(activeSkillDatas, effects);
            ApplyStatEnhance(activeSkillDatas, statEnhancerDatas);
            ApplyChanceEnhance(activeSkillDatas, chanceEnhancers);

            return skillSet;
        }

        private void ApplyStatEnhance(Dictionary<ActiveSkillData, RuntimeSkillEffect> activeSkillDatas, Queue<EffectValueEnhanceData> enhancers)
        {
            foreach (var enhancer in enhancers)
            {
                var value = activeSkillDatas[enhancer.TargetSkill];
                var effect = value.GetEffectBase(enhancer.TargetEffect);
                effect.AddStat(enhancer.TargetStatKey, enhancer.AddValue);
            }
        }

        private void ApplyChanceEnhance(Dictionary<ActiveSkillData, RuntimeSkillEffect> activeSkillDatas, Queue<EffectChanceEnhanceData> enhancers)
        {
            foreach (var enhancer in enhancers)
            {
                var value = activeSkillDatas[enhancer.TargetSkill];
                var effect = value.GetEffectBase(enhancer.TargetEffect);
                effect.AddChance(enhancer.AddChance);
            }
        }

        private void ApplyExtraEffect(Dictionary<ActiveSkillData, RuntimeSkillEffect> activeSkillDatas, Queue<ExtraEffectData> enhancers)
        {
            foreach (var enhancer in enhancers)
            {
                var value = activeSkillDatas[enhancer.TargetSkill];
                value.AddEffect(enhancer.EffectContainer, enhancer.Effect);
            }
        }

        public ActiveSkill CreateActiveSkill(ActiveSkillData skillSO, Hero owner, List<EffectBase> effects)
        {
            SkillExecutionContext executionContext = new SkillExecutionContext(owner, skillSO.AnimationData, skillSO.Execution, effects, skillSO.UID);

            ITrigger trigger = TriggerFactory.CreateTrigger(skillSO.Trigger);
            ITarget target = TargetFactory.CreateTarget(skillSO.Target, owner, _runtimeContext);
            IExecute execution = ExecutionFactory.CreateExecution(executionContext, _runtimeContext);

            ActiveSkill activeSkill = new ActiveSkill(skillSO.UID, owner, trigger, target, execution);

            return activeSkill;
        }
        
        private PassiveSkill CreatePassiveSkill(PassiveSkillData passiveSkillSO, Hero owner)
        {
            var effects = passiveSkillSO.Effects;

            return passiveSkillSO.Target switch
            {
                SelfTargetData => new SelfBuffPassive(owner.StatModify, effects),
                NearHeroTargetData => new NearHeroBuffPassive(_runtimeContext.FieldHero, owner, effects),
                AllHeroTargetData => new AllHeroBuffPassive(_runtimeContext.FieldHero, owner, effects),
                _ => null
            };
        }
    }
}
