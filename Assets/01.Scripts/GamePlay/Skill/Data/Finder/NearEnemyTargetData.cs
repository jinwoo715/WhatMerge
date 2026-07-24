using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "NearEnemyTarget", menuName = "Skill/Target/NearEnemyTarget", order = 0)]
    public class NearEnemyTargetData : FinderData 
    {
        public float Radius;
    }
   
}
