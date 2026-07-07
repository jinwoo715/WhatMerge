using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "UpgradeSet", menuName = "Skill/UpgradeSet", order = 0)]
    public class SkillSetContainer : ScriptableObject
    {
        public int UID;
        public List<UpgradeSet> Sets;

        public List<UpgradeSet> GetSets(int level)
        {
            List<UpgradeSet> sets = new List<UpgradeSet>();

            foreach (var set in Sets)
            {
                if (set.Level > level)
                    break;

                sets.Add(set);
            }

            return sets;
        }
    }

    [System.Serializable]
    public class UpgradeSet
    {
        public int Level;
        public SkillBaseData Skill;
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
        SkillStatEnhancer,
        SkillChanceEnhancer,
        ExtraEffect
    }
}
