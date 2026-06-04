using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Skill/Passive/Buff", order = 0)]
    public class PassiveSkillData : SkillBaseData
    {
        [Header("Å½»ö")]
        public TargetData Target;

        [Header("È¿°ú")]
        public List<BuffData> Effects;
    }
}