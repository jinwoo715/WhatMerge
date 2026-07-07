using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Dot", menuName = "Skill/Effect/Dot", order = 0)]
    public class DotEffect : DurationEffectBase
    {
        public float IntervalTime;
        public DotDamageType ApplyType;

        public float Value;
    }
    public enum DotDamageType
    {
        Fixed,
        CurrentHPRatio,
        MaxHPRatio
    }
}
