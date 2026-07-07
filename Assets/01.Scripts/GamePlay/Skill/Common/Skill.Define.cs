namespace Skill
{
    public enum EProjectileMoveType
    {
        Line,
        Homing,
        Parabola
    }

    public enum DebuffType
    {
        Slow,
        ArmorReduction
    }

    public enum SpawnPointType
    {
        Up,
        Right,
        Down,
        Left
    }

    public enum EProjectileEffectTrigger
    {
        OnHit,
        OnArrive,
        OnTimeOut,
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