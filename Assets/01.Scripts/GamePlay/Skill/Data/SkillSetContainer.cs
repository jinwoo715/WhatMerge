using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "UpgradeSet", menuName = "Skill/UpgradeSet", order = 0)]
    public class SkillSetContainer : ScriptableObject
    {
        public int UID;
        public List<HeroSkillSet> Sets;

        public List<HeroSkillSet> GetSets(int level)
        {
            List<HeroSkillSet> sets = new List<HeroSkillSet>();

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
    public class HeroSkillSet
    {
        public int Level;
        public SkillBaseData Skill;
    }
}
