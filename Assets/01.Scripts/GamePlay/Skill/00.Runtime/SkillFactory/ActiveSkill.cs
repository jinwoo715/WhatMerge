using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace Skill
{
    public class ActiveSkill : IActiveSkill
    {
        private Hero _owner;
        public ITrigger Trigger { get; private set; }
        public ITarget Target { get; private set; }
        public IExecute Execution { get; private set; }

        public int SkillUID { get; }
        public int SpawnIndex { get; }

        public ActiveSkill(int uid, Hero owner, ITrigger trigger, ITarget search, IExecute excution)
        {
            SkillUID = uid;
            _owner = owner;
            Trigger = trigger;
            Target = search;
            Execution = excution;
            SpawnIndex = owner.SpawnIndex;
        }
        public bool IsUsable(SkillTriggerContext context)
        {
            return Trigger.IsMeetTrigger(context) && Target.HasTargetInRange(_owner.Position);
        }
        public IEnumerator Execute()
        {
            IReadOnlyList<ICombatant> Targets = Target.GetTargets(_owner.Position);
            yield return Execution.Execute(Targets);
        }
        public void ModifyParam(EffectBase targetEffect, float value)
        {
            //Execution.EnhanceValue(targetEffect, value);
        }
        public void ModifyChance(EffectBase targetEffect, float value)
        {
            //Execution.EnhanceChance(targetEffect, value);
        }
        public void AddEffect(EffectBase effect)
        {
            //Execution.AddEffect(effect);
        }
    }
}
