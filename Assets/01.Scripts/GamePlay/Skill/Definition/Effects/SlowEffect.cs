using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Slow", menuName = "Skill/Effect/Slow", order = 0)]
    public class SlowEffect : DurationEffectBase
    {
        [Range(0, 1)]
        public float SlowRatio;
    }
}
