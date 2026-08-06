using WhatMerge.Combat;
using WhatMerge.Enemies;

namespace Skill
{
    public class SkillRuntimeContext
    {
        public ICombatService Combat { get; }
        public IFieldHeroService FieldHero { get; }
        public IFieldEnemyService FieldEnemy { get; }
        public SkillRuntimeContext(ICombatService combatService, IFieldHeroService fieldHeroService, IFieldEnemyService fieldEnemyService)
        {
            Combat = combatService;
            FieldEnemy = fieldEnemyService;
            FieldHero = fieldHeroService;
        }
    }
}
