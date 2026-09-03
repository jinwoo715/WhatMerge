namespace WhatMerge.Summons.Data
{
    public enum SummonExecutionTargetSource
    {
        SummonPosition = 0,
        TrackedTarget = 1
    }

    public class OnExpireExecutionSummon : SummonOnceExecution
    {
        public SummonExecutionTargetSource TargetSource = SummonExecutionTargetSource.SummonPosition;
    }
}
