using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class ExecutionSystemData : ScriptableObject
    {
        [Header("이펙트")]
        public List<EffectEntry> Effects;

        [Header("공격시 효과")]
        public VFXData VFX;
    }
}
