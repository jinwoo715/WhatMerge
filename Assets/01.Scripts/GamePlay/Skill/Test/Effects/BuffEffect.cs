using Heros;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "BuffEffect", menuName = "Skill/Effect/BuffEffect", order = 0)]

    public class BuffEffect : SkillEffect
    {
        public EHeroStatType BuffType;
        public int IncreaseRatio;
    }
}
