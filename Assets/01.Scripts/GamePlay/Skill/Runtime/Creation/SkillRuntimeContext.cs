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
        public IFatalStopService FatalStop { get; }

        public SkillRuntimeContext(
            ICombatService combatService,
            IFieldHeroService fieldHeroService,
            IFieldEnemyService fieldEnemyService,
            IVFXService vfxService,
            IFatalStopService fatalStopService)
        {
            Combat = combatService;
            FieldEnemy = fieldEnemyService;
            FieldHero = fieldHeroService;
            VFX = vfxService;
            FatalStop = fatalStopService;
        }
    }
}
