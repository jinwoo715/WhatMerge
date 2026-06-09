using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ConeEnemyTarget", menuName = "Skill/Target/ConeEnemyTarget", order = 0)]
    public class ConeEnemyTargetData : NearEnemyTargetBase
    {
        public float Angle;
    }
}
