using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat.Effects;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Skill/Passive/Buff", order = 0)]
    public class PassiveSkillData : SkillBaseData
    {
        [Header("탐색")]
        public HeroTargetData Target;

        [Header("효과")]
        public List<BuffData> Effects;
    }
}
