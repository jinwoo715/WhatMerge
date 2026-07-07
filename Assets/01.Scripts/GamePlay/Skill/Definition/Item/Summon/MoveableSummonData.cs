using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "MoveableSummon", menuName = "Skill/Item/Summon/Moveable", order = 0)]
    public class MoveableSummonData : SummonItemData
    {
        public SpawnPointType SpawnPoint;
        public float Speed;
    }
}
