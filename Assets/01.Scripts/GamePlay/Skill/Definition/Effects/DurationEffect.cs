using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class DurationEffect : EffectBase
    {
        public float Duration;
        public DurationEffectBase Effect;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}
