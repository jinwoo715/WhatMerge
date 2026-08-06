using System;
using WhatMerge.Enemies;

namespace WhatMerge.Stage
{
    public interface IMidBossChallengeInfo
    {
        event Action<Enemy, float, float> OnMidBossTimeChanged;
        event Action<Enemy> OnMidBossChallengeEnded;
    }
}
