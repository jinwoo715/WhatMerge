using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class RangeEffect : NomalEffect, IEffectContainer
    {
        public float Range;
        public List<EffectBase> Effects;
        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }


        public override void AddStat(string key, float value)
        {
            
        }
    }
}
