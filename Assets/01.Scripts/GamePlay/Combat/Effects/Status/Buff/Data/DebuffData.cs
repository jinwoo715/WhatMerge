using System;
using UnityEngine;
using UnityEngine.Serialization;
using WhatMerge.Enemies;

namespace WhatMerge.Combat.Effects
{
    [Serializable]
    public class DebuffData
    {
        [FormerlySerializedAs("DebuffType")]
        public EnemyStatType StatType;

        [FormerlySerializedAs("IncreaseRatio")]
        [Range(0f, 1f)]
        public float ReductionRatio;
    }
}
