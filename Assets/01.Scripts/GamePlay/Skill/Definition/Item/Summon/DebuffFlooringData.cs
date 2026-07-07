using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DebuffFlooring", menuName = "Skill/Item/Flooring/Debuff", order = 0)]
    public class DebuffFlooringData : FlooringBaseData
    {
        public DebuffType Type;

        [Range(0,1)]
        public float Ratio;
    }
}
