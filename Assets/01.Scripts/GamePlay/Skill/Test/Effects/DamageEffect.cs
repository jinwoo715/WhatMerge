using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Skill/Effect/DamageEffect", order = 0)]

    public class DamageEffect : SkillEffect
    {
        public int DamageRatio;
    }
}
