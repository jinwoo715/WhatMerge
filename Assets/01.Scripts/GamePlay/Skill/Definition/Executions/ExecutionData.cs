using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class ExecutionData : ScriptableObject, IEffectContainer
    {
        [Header("이펙트")]
        public List<EffectBase> Effects;

        [Header("공격시 효과")]
        public VFXData VFX;

        public List<EffectBase> GetEffects { get => Effects; set => Effects = value; }

    }
}
