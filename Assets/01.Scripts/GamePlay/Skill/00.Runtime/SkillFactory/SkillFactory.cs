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
        private SkillCommonContext _skillExecutionService;
        public void Init(SkillCommonContext skillExecutionService)
        {
            _skillExecutionService = skillExecutionService;
        }

        public SkillSet CreateSkill(Hero owner, int level, SkillSetContainer set)
        {
            var sets = set.GetSets(level);

            SkillSet skillSet = new SkillSet();

            Dictionary<ActiveSkillData, RuntimeSkillBuild> datas = new Dictionary<ActiveSkillData, RuntimeSkillBuild>();

            Queue<EffectStatEnhanceData> statEnhancerDatas = new Queue<EffectStatEnhanceData>();
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

                            var runtimeEffects = so.Execution?.Effects != null
                                ? new List<EffectBase>(so.Execution.Effects)
                                : new List<EffectBase>();

                            ActiveSkill skill = CreateActiveSkill(so, owner, runtimeEffects);
                            skillSet.ActiveSkills.Add(skill);

                            RuntimeSkillBuild runtimeSkillBuild = new RuntimeSkillBuild();
                            runtimeSkillBuild.SetEffects(runtimeEffects);
                            runtimeSkillBuild.Skill = skill;
                            runtimeSkillBuild.SourceSkill = so;

                            datas.Add(so, runtimeSkillBuild);
                        }

                        break;
                    case ESkillType.Passive:

                        PassiveSkillData passiveSO = Skill as PassiveSkillData;
                        PassiveSkill passive = CreatePassiveSkill(passiveSO, owner);
                        passive.SetUID(passiveSO.UID);

                        skillSet.PassiveSkills.Add(passive);

                        break;

                    case ESkillType.SkillStatEnhancer:

                        EffectStatEnhanceData skillEnhancerData = Skill as EffectStatEnhanceData;
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

            //TODO

            foreach (var statAdder in statEnhancerDatas)
            {
                if (datas.TryGetValue(statAdder.TargetSkill, out var skills))
                {
                    foreach (var slot in skills.Slots)
                    {
                        if (statAdder.TargetEffect == slot.Original)
                        {
                            var effect = slot.GetWritableEffect();

                            effect.AddStat(statAdder.AddValue);

                            break;
                        }
                    }
                }
            }

            foreach (var chacneAdder in chanceEnhancers)
            {
                if (datas.TryGetValue(chacneAdder.TargetSkill, out var skills))
                {
                    foreach (var slot in skills.Slots)
                    {
                        if (chacneAdder.TargetEffect == slot.Original)
                        {
                            var effect = slot.GetWritableEffect();

                            effect.AddChance(chacneAdder.AddChance);

                            break;
                        }
                    }
                }
            }

            foreach (var extra in effects)
            {
                if (datas.TryGetValue(extra.TargetSkill, out RuntimeSkillBuild skills))
                {
                    skills.ExtraEffect(extra.Effect);
                    Debug.Log("Extra");
                }
            }

            return skillSet;
        }

        private void ApplyStatEnhance() { }
        private void ApplyChanceEngance() { }
        private void ApplyExtraEffect() { }

        public ActiveSkill CreateActiveSkill(ActiveSkillData skillSO, Hero owner, List<EffectBase> effects)
        {
            ActiveSkillContext executionService = new ActiveSkillContext(owner, skillSO.AnimationData, skillSO.Execution, effects);

            ITrigger trigger = TriggerFactory.CreateTrigger(skillSO.Trigger);
            ITarget target = TargetFactory.CreateTarget(skillSO.Target, owner, _skillExecutionService);
            IExecute execution = ExecutionFactory.CreateExecution(executionService, _skillExecutionService);

            ActiveSkill activeSkill = new ActiveSkill(skillSO.UID, owner, trigger, target, execution);

            return activeSkill;
        }
        
        private IExecute GetExecution(ActiveSkillContext executionService)
        {
            return executionService.Execution switch
            {
                RandomMultiExecutionData => new RandomMultiExecution(executionService, _skillExecutionService),
                SequenceHitExecutionData => new SequenceExecution(executionService, _skillExecutionService),
                ConeExecutionData => new ConeExecution(executionService, _skillExecutionService),
                SingleExecutionData => new SingleExecution(executionService, _skillExecutionService),
                _ => new SingleExecution(executionService, _skillExecutionService),
            };
        }
        private ITarget GetTarget(TargetData data, Hero owner)
        {
            return data switch
            {
                SelfTargetData => new SelfTargetFinder(owner),
                NearHeroTargetData near => new NearHeroFinder(_skillExecutionService.FieldHeroService, owner, (int)near.TargetRange),
                AllHeroTargetData => new AllHeroFinder(_skillExecutionService.FieldHeroService),
                NearEnemyTargetData near => new SingleEnemyFinder(owner.transform, near.Radius),
                AllEnemyTargetData => new AllEnemyFinder(_skillExecutionService.FieldEnemyService),
                _ => null
            };
        }
        public ITrigger GetTrigger(TriggerData data)
        {
            return data switch
            {
                NoneTriggerData => new NoneTrigger(),
                HitCountTriggerData hitCount => new HitCountTrigger(hitCount.HitCount),
                ManaTriggerData mana => new ManaTrigger(mana.Mana),
                _ => null,
            };
        }
        
        private PassiveSkill CreatePassiveSkill(PassiveSkillData passiveSkillSO, Hero owner)
        {
            var effects = passiveSkillSO.Effects;

            return passiveSkillSO.Target switch
            {
                SelfTargetData => new SelfBuffPassive(owner.StatModify, effects),
                NearHeroTargetData => new NearHeroBuffPassive(_skillExecutionService.FieldHeroService, owner, effects),
                AllHeroTargetData => new AllHeroBuffPassive(_skillExecutionService.FieldHeroService, owner, effects),
                _ => null
            };
        }
    }
}
