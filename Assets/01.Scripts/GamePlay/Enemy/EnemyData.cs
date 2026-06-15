using WhatMerge.Combat;

namespace WhatMerge.Enemies
{
    [System.Serializable]
    public class EnemyData : BaseData
    {
        public string Name;
        public string Description;
        public float HP;
        public float Amour;
        public float MoveSpeed;
        public ElementType Attribute;
        public int Coin;
        public int SkillUID;
        public bool IsBoss;
    }
}