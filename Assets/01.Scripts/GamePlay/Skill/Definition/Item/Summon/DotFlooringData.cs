using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DotFlooring", menuName = "Skill/Item/Flooring/Dot", order = 0)]
    public class DotFlooringData : FlooringBaseData
    {
        public float Interval;
        public DotDamageType DamageType;
        public int DotDamage;
    }
}
