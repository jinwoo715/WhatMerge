using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace Skill
{
    public class ActiveSkill : IActiveSkill
    {
        private Hero _owner;
        private IReadOnlyList<ICombatant> _cachedTargets = Array.Empty<ICombatant>();

        public ITrigger Trigger { get; private set; }
        public IFinder Target { get; private set; }
        public IExecute Execution { get; private set; }

        public int SkillUID { get; }
        public int SpawnIndex { get; }

        public event Action OnDispose;

        public ActiveSkill(int uid, Hero owner, ITrigger trigger, IFinder search, IExecute excution)
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
            _cachedTargets = Array.Empty<ICombatant>();

            if (!Trigger.IsMeetTrigger(context))
            {
                return false;
            }

            return Target.TryGetTargets(_owner.Position, out _cachedTargets);
        }

        public IEnumerator Execute()
        {
            IReadOnlyList<ICombatant> targets = _cachedTargets;
            _cachedTargets = Array.Empty<ICombatant>();
            yield return Execution.Execute(targets);
        }

        public void Dispose()
        {
            _cachedTargets = Array.Empty<ICombatant>();
            OnDispose?.Invoke();
        }
    }
}
