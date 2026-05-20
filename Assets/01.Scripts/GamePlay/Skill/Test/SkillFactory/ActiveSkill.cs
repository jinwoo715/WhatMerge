using Combat;
using Entity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public interface IActiveSkill
    {
        public ITrigger Trigger { get; }
        public IFinder Search { get; }
        public IExecute Execution { get; }

        bool IsUsable(SkillTriggerContext context);
        IEnumerator Execute();
    }
    public class ActiveSkill : IActiveSkill
    {
        private Hero _owner;
        public ITrigger Trigger { get; private set; }
        public IFinder Search { get; private set; }
        public IExecute Execution { get; private set; }

        public ActiveSkill(Hero owner, ITrigger trigger, IFinder search, IExecute excution)
        {
            _owner = owner;
            Trigger = trigger;
            Search = search;
            Execution = excution;
            Debug.Log(Execution);
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
    }
}