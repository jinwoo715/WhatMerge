using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DecreaseAmour", menuName = "Skill/Effect/DecreaseAmour", order = 0)]
    public class ArmorReduction : DurationEffectBase
    {
        [Range(0,1)]
        public float Value;
    }
}
