using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "ProjectileAttack", menuName = "Skill/Execution/ProjectileAttack", order = 0)]
    public class ProjectileAttack : ExecutionSystem
    {
        public int ProjectileUID;
        public EShootType ShootType;
        public int TypeParam;
    }

    public enum EShootType
    {
        Single,
        Multi,
        Sequence
    }
}
