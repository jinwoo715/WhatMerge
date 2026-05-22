using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Skill/Execution/ProjectileSkill", order = 0)]
    public class ProjectileSkill : ExecutionSystemData
    {
        public ProjectileDataSO ProjectileData;
    }
}
