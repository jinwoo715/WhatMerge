using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Melee", menuName = "Skill/Melee", order = 0)]
    public class MeleeAttack : ExecutionSystem
    {
        public bool IsRangeAttack;
        public float Angle;
    }
}
