using WhatMerge.Enemies;
using Skill.Data;
using Skill.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace Skill
{

    public class SkillCommonContext
    {
        public ICombatService CombatService { get; }
        public IFieldHeroService FieldHeroService { get; }
        public IFieldEnemyService FieldEnemyService { get; }

        public SkillCommonContext(ICombatService combatService, IFieldHeroService fieldHeroService, IFieldEnemyService fieldEnemyService)
        {
            CombatService = combatService;
            FieldEnemyService = fieldEnemyService;
            FieldHeroService = fieldHeroService;
        }
    }
    public class ActiveSkillContext
    {
        public Hero Hero { get; }
        public ISpriteChanger SpriteChanger { get; }
        public SkillAnimationData AnimationData { get; }
        public ExecutionData Execution { get; }
        public List<EffectBase> RuntimeEffects { get; }
        public ActiveSkillContext(Hero hero, SkillAnimationData animationData, ExecutionData execution, List<EffectBase> effects)
        {
            Hero = hero;
            AnimationData = animationData;
            Execution = execution;
            RuntimeEffects = effects;
            SpriteChanger = hero.GetComponent<ISpriteChanger>();
        }
    }
    
    
    
}
     