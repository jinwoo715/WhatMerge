using System.Collections.Generic;
using UnityEngine;
using System;
using Skill.Data;

namespace WhatMerge.Summons.Data
{
    public class SummonExecutionData : ScriptableObject, IEffectContainer
    {
        public List<EffectBase> GetEffects { get => GetEffectList(); }

        public virtual List<EffectBase> GetEffectList() { throw new NotImplementedException(); }
        public virtual void SetEffects(List<EffectBase> effectBases) { }
        public virtual void AddEffect(EffectBase effect) { throw new NotImplementedException(); }
    }
}
