using Combat;
using System;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Summons.Data;

namespace WhatMerge.Summons
{
    public interface ISummonMoveStrategy
    {
        event Action<TargetLostEventType> OnTargetLost;
        void Tick(float tick);
    }


}
