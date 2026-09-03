using Skill.Data;
using UnityEngine;

namespace WhatMerge.Enemies.Skills.Data
{
    [CreateAssetMenu(fileName = "EnemySkillVFXEffect", menuName = "Enemy Skill/Effect/VFX", order = 5)]
    public sealed class EnemySkillVFXEffectData : EnemySkillEffectData
    {
        public override bool RequiresTarget => VFX != null
            && (VFX.PositionType == VFXSpawnPositionTpye.Target
                || VFX.PositionType == VFXSpawnPositionTpye.Middle);

        public override EnemySkillEffectTargetType TargetType => EnemySkillEffectTargetType.Any;
    }
}
