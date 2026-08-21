using UnityEngine;

namespace Skill.Data
{
    public enum HeroSearchType
    {
        Single,
        Cross,
        Surrounding,
        All
    }

    [CreateAssetMenu(fileName = "NearHeroTarget", menuName = "Skill/Target/NearHeroTarget", order = 0)]
    public class NearHeroTargetData : HeroTargetData
    {
        public HeroSearchType TargetRange;
        public bool IncludeSelf = true;
    }
}
