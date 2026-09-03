using Skill.Data;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    public enum EnemySkillEffectTargetType
    {
        Any,
        Enemy,
        Hero
    }

    public abstract class EnemySkillEffectData : ScriptableObject
    {
        [Range(0f, 1f)]
        public float Chance = 1f;

        public VFXData VFX;

        public virtual bool RequiresTarget => true;
        public abstract EnemySkillEffectTargetType TargetType { get; }
    }
}
