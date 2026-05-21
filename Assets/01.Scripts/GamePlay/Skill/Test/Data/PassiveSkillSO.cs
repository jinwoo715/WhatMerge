using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Passive Skill", menuName = "Skill/Passive", order = 0)]
    public class PassiveSkillSO : SkillBase
    {
        [Header("Å½»ö")]
        public TargetSystem Target;

        [Header("È¿°ú")]
        public List<EffectBase> Effects;
    }
}