using Combat;
using Entity;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class ActiveSkill : IActiveSkill
    {
        private Hero _owner;
        public ITrigger Trigger { get; private set; }
        public ITarget Search { get; private set; }
        public IExecute Execution { get; private set; }

        public int UID { get; }

        public ActiveSkill(int uid, Hero owner, ITrigger trigger, ITarget search, IExecute excution)
        {
            UID = uid;
            _owner = owner;
            Trigger = trigger;
            Search = search;
            Execution = excution;
        }
        public bool IsUsable(SkillTriggerContext context)
        {
            return Trigger.IsMeetTrigger(context) && Search.HasTargetInRange(_owner.Position);
        }
        public IEnumerator Execute()
        {
            IReadOnlyList<Creature> Targets = Search.GetTargets(_owner.Position);
            yield return Execution.Execute(Targets);
        }
        public void ModifyParam(int effectIndex, float value)
        {
           
        }
        public void ModifyChance(int effectIndex, float value)
        {
            
        }
        public void AddEffect(EffectEntry effect)
        {
            Execution.AddEffect(effect);
        }
    }
}