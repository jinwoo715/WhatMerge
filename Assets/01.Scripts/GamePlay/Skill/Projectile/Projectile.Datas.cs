namespace Skill.Projectile
{
    [System.Serializable]
    public class ProjectileData
    {
        public int ProjectileUID;
        public string SpriteName;
        public EProjectileMoveType MoveType;
        public float Speed;
        public float LifeTime;
        public EProjectileAttackType TargetType;
        public EProjectileTrigger DestoryType;
    }
}