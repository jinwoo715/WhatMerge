using Combat;
using Entity;
using Skill.Data;
using System.Collections.Generic;

namespace Skill
{
    public class SkillPayload
    {
        public Hero Attacker;
        public ICreature Target;
        public List<EffectBase> effects;
        public AttackPayload payLoad;
    }
}