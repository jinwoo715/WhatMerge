using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    public enum EnemySkillTargetCategory
    {
        Enemy,
        Hero
    }

    public abstract class EnemySkillTargetData : ScriptableObject
    {
        public abstract EnemySkillTargetCategory Category { get; }
    }
}
