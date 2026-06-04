using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "SummonEffect", menuName = "Skill/Effect/SummonEffect", order = 0)]
    public class SummonEffect : EffectBase
    {
        public SummonData Summon;
    }
}

