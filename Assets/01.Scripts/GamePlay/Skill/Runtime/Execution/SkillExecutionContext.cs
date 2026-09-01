using Skill.Data;
using System.Collections.Generic;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace Skill
{
    public class SkillExecutionContext
    {
        public Hero Hero { get; }
        public ISpriteChanger SpriteChanger { get; }
        public SkillAnimationData AnimationData { get; }
        public ExecutionData ExecutionData { get; }
        public List<EffectBase> Effects { get; }
        public float ChargeTime { get; }
        public IRuntimeEffectLifetime EffectLifetime { get; }
        public IFinder Finder { get; }

        public SkillExecutionContext(
            Hero hero,
            SkillAnimationData animationData,
            ExecutionData executionData,
            float chargeTime,
            IRuntimeEffectLifetime effectLifetime,
            IFinder finder)
        {
            Hero = hero;
            AnimationData = animationData;
            ExecutionData = executionData;
            Effects = executionData.Effects;
            SpriteChanger = hero.GetComponent<ISpriteChanger>();
            ChargeTime = chargeTime;
            EffectLifetime = effectLifetime;
            Finder = finder;
        }
    }
}
