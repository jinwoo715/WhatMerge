using System;
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
            if (Sets == null)
                throw new InvalidOperationException($"Skill set container '{name}' has no set list.");

            List<HeroSkillSet> sets = new List<HeroSkillSet>();
            int previousLevel = int.MinValue;

            for (int i = 0; i < Sets.Count; i++)
            {
                HeroSkillSet set = Sets[i];

                if (set == null)
                    throw new InvalidOperationException($"Skill set container '{name}' has a null entry at index {i}.");

                if (set.Level < previousLevel)
                {
                    throw new InvalidOperationException(
                        $"Skill set container '{name}' must be ordered by level. " +
                        $"Index {i - 1}: {previousLevel}, index {i}: {set.Level}.");
                }

                previousLevel = set.Level;

                if (set.Level <= level)
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
