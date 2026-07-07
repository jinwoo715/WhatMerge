using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Stun", menuName = "Skill/Effect/Stun", order = 0)]
    public class StunEffect : DurationEffectBase
    {
        public float StunTime;
    }
}
