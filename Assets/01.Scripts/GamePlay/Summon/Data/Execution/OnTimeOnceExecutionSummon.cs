using UnityEngine;

namespace WhatMerge.Summons.Data
{
    public class OnTimeOnceExecutionSummon : SummonOnceExecution
    {
        [Range(0, 1)] public float ExecutionTiming;
    }
}
