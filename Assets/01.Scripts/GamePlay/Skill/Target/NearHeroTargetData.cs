using UnityEngine;

namespace Skill.Data
{
    public enum ENearHeroTargetRange
    {
        Near = 1,
        Far = 2,
    }

    [CreateAssetMenu(fileName = "NearHeroTarget", menuName = "Skill/Target/NearHeroTarget", order = 0)]
    public class NearHeroTargetData : HeroTargetData
    {
        public ENearHeroTargetRange TargetRange;
    }
}
