using System;
using WhatMerge.Summons.Data;

namespace WhatMerge.Summons
{
    public interface ISummonMoveStrategy
    {
        event Action<TargetLostEventType> OnTargetLost;
        void Tick(float tick);
    }
}
