using WhatMerge.Combat;

namespace WhatMerge.Enemies
{
    [System.Serializable]
    public class EnemyData : BaseData
    {
        public string Name;
        public string Description;
        public string SpriteKey;
        public EnemyType EnemyType;
        public float MaxHP;
        public float Armor;
        public float MoveSpeed;
        public ElementType Attribute;
        public int SkillSetUID;
        public int KillGold;
        public int RewardGroupUID;
    }
}
