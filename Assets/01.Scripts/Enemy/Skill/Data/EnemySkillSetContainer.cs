using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemySkillSet", menuName = "Enemy Skill/Skill Set", order = 1)]
    public sealed class EnemySkillSetContainer : ScriptableObject
    {
        [Min(1)]
        public int UID;

        public List<EnemySkillData> Skills = new List<EnemySkillData>();
    }
}
