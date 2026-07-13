using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Data 
{
    [CreateAssetMenu(fileName = "Element", menuName = "Skill/Effect/Element", order = 0)]
    public class ElementEffect : DurationEffectBase
    {
        public ElementType Attribute;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}