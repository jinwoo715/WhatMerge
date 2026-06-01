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
}