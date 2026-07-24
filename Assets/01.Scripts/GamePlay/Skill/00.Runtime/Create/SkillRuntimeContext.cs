using WhatMerge.Combat;
using WhatMerge.Enemies;

namespace Skill
{
    public class SkillRuntimeContext
    {
        public ICombatService Combat { get; }
        public IFieldHeroService FieldHero { get; }
        public IFieldEnemyService FieldEnemy { get; }
        public IVFXService VFX { get; }

        public SkillRuntimeContext(ICombatService combatService, IFieldHeroService fieldHeroService, IFieldEnemyService fieldEnemyService, IVFXService vFXService)
        {
            Combat = combatService;
            FieldEnemy = fieldEnemyService;
            FieldHero = fieldHeroService;
            VFX = vFXService;
        }
    }
}
