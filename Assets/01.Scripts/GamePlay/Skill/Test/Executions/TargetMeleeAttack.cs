using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "TargetMelee", menuName = "Skill/Execution/TargetMelee", order = 0)]
    public class TargetMeleeAttack : ExecutionSystem
    {
        public int HitCount;
    }
}
