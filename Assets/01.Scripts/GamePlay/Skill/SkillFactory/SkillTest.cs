using WhatMerge.Enemies;
using Skill.Data;
using Skill.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace Skill
{
    public class SkillSet
    {
        public List<IActiveSkill> ActiveSkills = new List<IActiveSkill>();
        public List<IPassiveSkill> PassiveSkills = new List<IPassiveSkill>();
    }
    public class SkillCommonContext
    {
        public IProjectileProvider Projectile { get; }
        public ICombatService CombatService { get; }
        public IFieldHeroService FieldHeroService { get; }
        public IFieldEnemyService FieldEnemyService { get; }

        public SkillCommonContext(IProjectileProvider projectile, ICombatService combatService, IFieldHeroService fieldHeroService, IFieldEnemyService fieldEnemyService)
        {
            Projectile = projectile;
            CombatService = combatService;
            FieldEnemyService = fieldEnemyService;
            FieldHeroService = fieldHeroService;
        }
    }
    public class ActiveSkillContext
    {
        public Hero Hero { get; }
        public ISpriteChanger SpriteChanger { get; }
        public SkillAnimationData AnimationData { get; }
        public ExecutionSystemData System { get; }
        public List<EffectBase> RuntimeEffects { get; }
        public ActiveSkillContext(Hero hero, SkillAnimationData animationData, ExecutionSystemData system, List<EffectBase> effects)
        {
            Hero = hero;
            AnimationData = animationData;
            System = system;
            RuntimeEffects = effects;
            SpriteChanger = hero.GetComponent<ISpriteChanger>();
        }
    }

    public class RuntimeSkillBuild
    {
        public ActiveSkillData SourceSkill;
        public ActiveSkill Skill;
        public List<EffectBase> Effects = new List<EffectBase>();
        public List<RuntimeEffectSlot> Slots = new List<RuntimeEffectSlot>();
        private readonly Dictionary<int, SummonEffect> _writableSummons = new Dictionary<int, SummonEffect>();

        public void SetEffects(List<EffectBase> effects)
        {
            Effects = effects;

            for (int i = 0; i < effects.Count; i++)
            {
                int rootIndex = i;
                EffectBase rootEffect = Effects[rootIndex];

                AddRootSlot(rootIndex, rootEffect);

                if(rootEffect is SummonEffect summonEffect)
                {
                    AddSummonSlots(rootIndex, summonEffect);
                }

                var slot = new RuntimeEffectSlot(Effects[i]);
                slot.Replace = (effect) => ReplaceEffect(rootIndex, effect);
                Slots.Add(slot);
            }
        }

        private void AddSummonSlots(int rootIndex, SummonEffect summonEffect)
        {
            if (summonEffect.Summon?.Effects == null)
                return;

            var effects = summonEffect.Summon.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                int innerIndex = i;
                EffectBase innerEffect = effects[innerIndex];

                var slot = new RuntimeEffectSlot(innerEffect);
                slot.Replace = copiedEffect =>
                {
                    SummonEffect writableSummonEffect = EnsureWritableSummonEffect(rootIndex);
                    writableSummonEffect.Summon.Effects[innerIndex] = copiedEffect;
                };

                Slots.Add(slot);
            }
        }

        private SummonEffect EnsureWritableSummonEffect(int rootIndex)
        {
            if (_writableSummons.TryGetValue(rootIndex, out var cached))
                return cached;

            SummonEffect current = Effects[rootIndex] as SummonEffect;

            if (current == null || current.Summon == null)
                return null;

            SummonEffect copiedSummonEffect = UnityEngine.Object.Instantiate(current);

            SummonData copiedSummonData = UnityEngine.Object.Instantiate(current.Summon);
            copiedSummonData.Effects = new List<EffectBase>(current.Summon.Effects);

            copiedSummonEffect.Summon = copiedSummonData;
            Effects[rootIndex] = copiedSummonEffect;

            _writableSummons.Add(rootIndex, copiedSummonEffect);
            return copiedSummonEffect;
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
            if(Current == null)
            {
                Current = UnityEngine.Object.Instantiate(Original);
                Replace(Current);
            }

            return Current;
        }
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
                        ActiveSkillData so = Skill as ActiveSkillData;

                        Debug.Log(so);
                        Debug.Log(so.Execution);
                        Debug.Log(so.Execution.Effects);
                        var runtimeEffects = new List<EffectBase>(so.Execution.Effects);

                        ActiveSkill skill = CreateActiveSkill(so, owner, runtimeEffects);
                        skillSet.ActiveSkills.Add(skill);

                        RuntimeSkillBuild runtimeSkillBuild = new RuntimeSkillBuild();
                        runtimeSkillBuild.SetEffects(runtimeEffects);
                        runtimeSkillBuild.Skill = skill;
                        runtimeSkillBuild.SourceSkill = so;

                        datas.Add(so, runtimeSkillBuild);

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
                        if(statAdder.TargetEffect == slot.Original)
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
        private ActiveSkill CreateActiveSkill(ActiveSkillData skillSO, Hero owner, List<EffectBase> effects)
        {
            ITrigger trigger = GetTrigger(skillSO.Trigger);
            ITarget target = GetTarget(skillSO.Target, owner);

            ActiveSkillContext executionService = new ActiveSkillContext(owner, skillSO.AnimationData, skillSO.Execution, effects);
            IExecute execution = GetExecution(executionService);
            
            ActiveSkill activeSkill = new ActiveSkill(skillSO.UID, owner, trigger, target, execution);

            return activeSkill;
        }
        public ITrigger GetTrigger(TriggerData system)
        {
            return system switch
            {
                NoneTriggerData => new NoneRequire(),
                HitCountTriggerData hitCount => new HitCountRequire(hitCount.HitCount),
                ManaTriggerData mana => new ManaRequire(mana.Mana),
                _ => null,
            };
        }

        private ITarget GetTarget(TargetData system, Hero owner)
        {
            return system switch
            {
                SelfTargetData => new SelfTargetFinder(owner),
                NearHeroTargetData near => new NearHeroFinder(_skillExecutionService.FieldHeroService, owner, (int)near.TargetRange),
                AllHeroTargetData => new AllHeroFinder(_skillExecutionService.FieldHeroService),
                SingleEnemyTargetData near => new SingleEnemyFinder(owner.transform, near.Radius),
                ConeEnemyTargetData coneNear => new ConeEnemyFinder(owner.transform, coneNear.Radius, coneNear.Angle),
                AllEnemyTargetData => new AllEnemyFinder(_skillExecutionService.FieldEnemyService),
                _=> null
            };
        }

        //TODO 스킬 생성 수정
        private IExecute GetExecution(ActiveSkillContext executionService)
        {
            string name = string.Empty;

            string skillName = executionService.System.name;
            if (skillName.Contains("MeleeAttack"))
            {
                name = "Skill.MeleeExecution";
            }
            else if (skillName.Contains("Projectile"))
            {
                name = "Skill.TargetProjectile";
            }

            Debug.Log($"{skillName} / {name}");

            Type type = Type.GetType(name);

            Debug.Log(type);

            if (type != null)
            {
                object[] args = new object[] { executionService, _skillExecutionService };

               return (IExecute)Activator.CreateInstance(type, args);
                
            }
            else
            {
                return null;
            }
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
     