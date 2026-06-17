using Combat;
using WhatMerge.Heros;
using Skill.Data;
using System.Collections.Generic;
using WhatMerge.Combat;

namespace Skill
{
    public class SkillPayload
    {
        public Hero Attacker;
        public ICombatant Target;
        public List<EffectBase> effects;
        public AttackPayload payLoad;
    }
}
