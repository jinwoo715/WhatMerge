using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Melee", menuName = "Skill/Execution/SingleTargetMelee", order = 0)]
    public class TargetMeleeAttack : ExecutionSystem
    {
        public int HitCount;
    }
}
