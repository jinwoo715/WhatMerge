using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class SkillEnhancer : SkillBase
    {
        [Header("강화될 스킬")]
        public SkillBase TargetSkill;
    }

    //스킬에 추가 이펙트 넣기
    //이펙트 값 강화하기

    //액티브 스킬 강화 -> 액티브 스킬 안의 특정 이펙트 강화
    //패시브 스킬 강화 -> 패시브 스킬 안의 특정 이펙트 강화
}