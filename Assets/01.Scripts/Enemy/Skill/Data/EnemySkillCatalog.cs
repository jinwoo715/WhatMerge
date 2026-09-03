using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemySkillCatalog", menuName = "Enemy Skill/Catalog", order = 0)]
    public sealed class EnemySkillCatalog : ScriptableObject
    {
        public List<EnemySkillSetContainer> SkillSets = new List<EnemySkillSetContainer>();

        public bool TryGetSkillSet(int uid, out EnemySkillSetContainer skillSet)
        {
            if (SkillSets != null)
            {
                for (int i = 0; i < SkillSets.Count; i++)
                {
                    EnemySkillSetContainer candidate = SkillSets[i];
                    if (candidate != null && candidate.UID == uid)
                    {
                        skillSet = candidate;
                        return true;
                    }
                }
            }

            skillSet = null;
            return false;
        }

        public EnemySkillSetContainer GetSkillSet(int uid)
        {
            if (TryGetSkillSet(uid, out EnemySkillSetContainer skillSet))
                return skillSet;

            throw new KeyNotFoundException($"Enemy skill set UID {uid} is not registered in '{name}'.");
        }
    }
}
