using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class ProjectileSkill : AttackSkill
    {
        private Transform _ownerTransform;
        private Transform _target;

        public ProjectileSkill(ActiveSkillData data, IServiceLocator context, IServiceLocator owner, ISkillTriggerStrategy trigger) : base(data, context, owner, trigger) { }

        public override void BindService()
        {
            
        }

        public override IEnumerator Execute()
        {
            yield break;
        }

        public override bool HasValidTarget()
        {
            float radius = _statProvider.GetStat(EAttackStatType.Radius);

            if (_target != null)
            {
                float dist = Vector2.Distance(_target.transform.position, _ownerTransform.position);

                if (dist > radius)
                    _target = null;
            }

            if (_target == null)
            {
                if (CreatureFinder.TryFindNearEnemyTransform(_ownerTransform.position, radius, out var target))
                {
                    _target = target;
                    return true;
                }
                else
                    return false;
            }

            return true;
        }

    }
}
