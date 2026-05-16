namespace Enemies
{
    [System.Serializable]
    public class EnemyData : BaseData
    {
        public string Name;
        public string Description;
        public float HP;
        public float Amour;
        public float MoveSpeed;
        public EAttribute Attribute;
        public int Coin;
        public int SkillUID;
        public bool IsBoss;
    }
}