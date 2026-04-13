using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{

    public class ProjectileSkill : ActiveSkillBase
    {
        public ProjectileSkill(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

        public override void BindService()
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerator Excute()
        {
            throw new System.NotImplementedException();
        }

        public override bool HasValidTarget()
        {
            throw new System.NotImplementedException();
        }
    }
}
