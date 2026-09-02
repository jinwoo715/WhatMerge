using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using WhatMerge.Combat.Effects;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Debuff", menuName = "Skill/Passive/Debuff", order = 0)]
    public class PassiveDebuffSkillData : PassiveSkillData
    {
        [Header("탐색")]
        [FormerlySerializedAs("FindData")]
        public EnemyTargetData Target;

        [Header("효과")]
        public List<DebuffData> Effects;
    }
}
