using Skill.Data;
using System.Collections.Generic;
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
        public int SkillUid { get; }

        public SkillExecutionContext(Hero hero, SkillAnimationData animationData, ExecutionData executionData, List<EffectBase> effects, int skillUid)
        {
            Hero = hero;
            AnimationData = animationData;
            ExecutionData = executionData;
            Effects = effects ?? new List<EffectBase>();
            SpriteChanger = hero.GetComponent<ISpriteChanger>();
            SkillUid = skillUid;
        }
    }
}
