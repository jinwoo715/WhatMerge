using Skill.Data;
using WhatMerge.Heros;

namespace Skill
{
    public class TargetFactory
    {
        public static ITarget CreateTarget(TargetData data, Hero owner, SkillRuntimeContext runtimeContext)
        {
            return data switch
            {
                SelfTargetData => new SelfTargetFinder(owner),
                NearHeroTargetData near => new NearHeroFinder(runtimeContext.FieldHero, owner, (int)near.TargetRange),
                AllHeroTargetData => new AllHeroFinder(runtimeContext.FieldHero),
                NearEnemyTargetData near => new SingleEnemyFinder(owner.transform, near.Radius),
                AllEnemyTargetData => new AllEnemyFinder(runtimeContext.FieldEnemy),
                _ => null
            };
        }
    }
}
