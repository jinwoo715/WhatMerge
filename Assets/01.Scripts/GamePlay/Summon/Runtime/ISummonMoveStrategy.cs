using System;
using WhatMerge.Summons.Data;

namespace WhatMerge.Summons
{
    public interface ISummonMoveStrategy : IDisposable
    {
        event Action<TargetLostEventType> OnTargetLost;
        void Tick(float tick);
    }
}
