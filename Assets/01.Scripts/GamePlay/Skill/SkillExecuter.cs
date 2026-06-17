using Skill.Data;
using UnityEngine;
using Skill.Projectile;
using WhatMerge.Enemies;
using WhatMerge.Combat;

namespace Skill
{
    public abstract class SkillExecuter
    {
        protected ICombatService _combatService;

        public SkillExecuter(ICombatService combatService)
        {
            _combatService = combatService;
        }
        public abstract void Execute(SkillImpactContext impactContext, EffectTargetData targetResolveType, SkillPayload payload);
    }

    public class SingleTargetExecuter : SkillExecuter
    {
        public SingleTargetExecuter(ICombatService combatService) : base(combatService) { }

        public override void Execute(SkillImpactContext impactContext, EffectTargetData targetResolveType, SkillPayload payload)
        {
            DamageContext context = new DamageContext(payload.payLoad, impactContext.ImpactTarget, payload.Attacker);
            context.skillEffects = payload.effects;
            _combatService.RegisterAttack(context);
        }
    }
    public class AreaTargetExecuter : SkillExecuter
    {
        public AreaTargetExecuter(ICombatService combatService) : base(combatService) { }

        public override void Execute(SkillImpactContext impactContext, EffectTargetData targetResolveType, SkillPayload payload)
        {
            float radius = (targetResolveType as AreaEffectTargetData).Radius;

            var enemies = SearchUtility.GetNearEnemies(impactContext.ImpactPosition, radius);

            foreach (var enemy in enemies)
            {
                DamageContext context = new DamageContext(payload.payLoad, enemy, payload.Attacker);
                context.skillEffects = payload.effects;
                _combatService.RegisterAttack(context);
            }
        }
    }
}