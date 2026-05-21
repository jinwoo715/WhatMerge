namespace Skill
{
    public enum ESkillTriggerType
    {
        None,
        HitCount,
        Mana,
    }

    public enum ESkillTargetType
    {
        Self,
        NearHeros,
        AllHeros,
        NearEnemies,
        AllEnemies,
    }
    public enum EBuffTargetType
    {
        Self,
        NearHeros,
        AllHeros
    }

    public enum EExtraAttackEffectType
    {
        IgnoreAmour,
        StatusEffect,
    }
    public enum EProjectileAttackType
    {
        Single,
        Multiple,
        Summon
    }

    public enum EProjectileMoveType
    {
        Line,
        Homing,
        Parabola
    }
    public struct SkillTriggerContext
    {
        public int HitCount;
        public float Mana;

        public SkillTriggerContext(int hitCount, float mana)
        {
            HitCount = hitCount;
            Mana = mana;
        }
    }
}