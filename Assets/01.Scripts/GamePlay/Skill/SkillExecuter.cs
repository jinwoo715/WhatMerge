using Combat;
using Enemies;
using Skill.Data;
using UnityEngine;
using Skill.Projectile;
using WhatMerge.Enemies;
using WhatMerge.Combat;

namespace Skill
{
    public class SkillExecuter
    {
        private ICombatService _combatService;
        private SkillPayload _payload;
        private TargetResolveData _targetResolveType;

        public SkillExecuter(ICombatService combatService)
        {
            _combatService = combatService;
        }

        public void SetData(TargetResolveData targetResolveType, SkillPayload payload)
        {
            _targetResolveType = targetResolveType;
            _payload = payload;
        }

        //TODO
        public void Execute(SkillImpactContext impactContext)
        {
            if (_targetResolveType.Type == ETargetResolveType.Single)
            {
                DamageContext context = new DamageContext(_payload.payLoad, impactContext.ImpactTarget, _payload.Attacker);
                context.skillEffects = _payload.effects;
                _combatService.RegisterAttack(context);
            }
            else if (_targetResolveType.Type == ETargetResolveType.Area)
            {
                var enemies = SearchUtility.GetNearAll2DTargets<Enemy>(impactContext.ImpactPosition, _targetResolveType.Radius, LayerMask.GetMask("Enemy"));

                foreach (var enemy in enemies)
                {
                    DamageContext context = new DamageContext(_payload.payLoad, enemy, _payload.Attacker);
                    context.skillEffects = _payload.effects;
                    _combatService.RegisterAttack(context);
                }
            }
        }
    }
}