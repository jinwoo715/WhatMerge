using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Passive Skill", menuName = "Skill/Passive", order = 0)]
    public class PassiveSkillSO : SkillBase
    {
        [Header("Info")]
        public string Name;
        public string Description;

        [Header("Å½»ö")]
        public TargetSystem Target;

        [Header("È¿°ú")]
        public List<SkillEffect> Effects;
    }
}