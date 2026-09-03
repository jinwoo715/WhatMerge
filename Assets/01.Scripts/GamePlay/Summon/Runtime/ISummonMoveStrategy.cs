using System;
using WhatMerge.Combat;
using WhatMerge.Summons.Data;

namespace WhatMerge.Summons
{
    public interface ISummonMoveStrategy : IDisposable
    {
        event Action<TargetLostEventType> OnTargetLost;
        void Tick(float tick);
    }

    public interface ISummonTargetProvider
    {
        bool TryGetActiveTarget(out ICombatant target);
    }
}
