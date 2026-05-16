using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "UpgradeSet", menuName = "Skill/UpgradeSet", order = 0)]
    public class HeroUpgradeSkillSet : ScriptableObject
    {
        public int UID;
        public List<UpgradeSet> Sets;
    }

    [System.Serializable]
    public class UpgradeSet
    {
        public int Level;
        public SkillType Type;
    }

    [System.Serializable]
    public class SkillType
    {
        public ESkillType Skill;
        public int UID;
    }

    public enum ESkillType
    {
        Active,
        Passive,
        Enhancer
    }
}
