using Skill.Data;
using System;
using WhatMerge.Heros;

namespace Skill
{
    public class FinderFactory
    {
        public static IFinder CreateTarget(FinderData data, Hero owner, SkillRuntimeContext runtimeContext)
        {
            return data switch
            {
                SelfTargetData => new SelfTargetFinder(owner),
                NearHeroTargetData near => new NearHeroFinder(runtimeContext.FieldHero, owner, (int)near.TargetRange),
                AllHeroTargetData => new AllHeroFinder(runtimeContext.FieldHero),
                NearEnemyTargetData near => new NearEnemyFinder(near.Radius),
                AllEnemyTargetData => new AllEnemyFinder(runtimeContext.FieldEnemy),
                _ => throw new InvalidOperationException($"Unsupported TargetData: {data?.name ?? "null"}")
            };
        }
    }
}
