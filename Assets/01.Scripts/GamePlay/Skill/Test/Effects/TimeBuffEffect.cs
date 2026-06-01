using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "TimeBuffEffect", menuName = "Skill/Effect/TimeBuffEffect", order = 0)]
    public class TimeBuffEffect : BuffEffect
    {
        public float Time;
    }
}