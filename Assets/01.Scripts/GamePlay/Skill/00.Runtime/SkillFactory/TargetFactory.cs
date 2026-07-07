using Skill.Data;
using WhatMerge.Heros;

namespace Skill
{
    public class TargetFactory
    {
        public static ITarget CreateTarget(TargetData data, Hero owner, SkillCommonContext _skillExecutionService)
        {
            return data switch
            {
                SelfTargetData => new SelfTargetFinder(owner),
                NearHeroTargetData near => new NearHeroFinder(_skillExecutionService.FieldHeroService, owner, (int)near.TargetRange),
                AllHeroTargetData => new AllHeroFinder(_skillExecutionService.FieldHeroService),
                NearEnemyTargetData near => new SingleEnemyFinder(owner.transform, near.Radius),
                AllEnemyTargetData => new AllEnemyFinder(_skillExecutionService.FieldEnemyService),
                _ => null
            };
        }
    }
}
