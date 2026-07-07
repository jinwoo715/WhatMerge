using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Skill/Effect/DamageEffect", order = 0)]

    public class DamageEffect : EffectBase
    {
        public float DamageRatio;

        public override void AddStat(float value)
        {
            DamageRatio += value;
        }
    }
}
