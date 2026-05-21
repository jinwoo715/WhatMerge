using Enemies;
using Heros;

namespace Skill
{
    [System.Serializable]
    public class BuffData : BaseData
    {
        public EBuffTargetType TargetType;
        public EHeroStatType StatType;
        public int Value;
    }

    [System.Serializable]
    public class DeBuffData : BaseData
    {
        public EEnemyStatType StatType;
        public int Value;
    }

    [System.Serializable]
    public class ExtraEffectData : BaseData
    {
        public int AttachedActiveSkillUID;
        public EExtraAttackEffectType EffectType;
        public int Chance;
        public int StatusEffectUID;
    }

    [System.Serializable]
    public class ProjectileData
    {
        public int ProjectileUID;
        public string SpriteName;
        public EProjectileMoveType MoveType;
        public float Speed;
        public float LifeTime;
        public bool LevelSwap;
        public EProjectileAttackType TargetType;
        public EProjectileTrigger DestoryType;
    }
}