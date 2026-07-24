using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class ExecutionEffect : EffectBase
    {
        //%이하 처형
        [Range(0,1)]
        public float ExecuteThreshold;
        public override void AddStat(string key, float value)
        {
        }
    }
}
