using System;

namespace WhatMerge.Stage
{
    public interface IWaveInfoProvider
    {
        event Action<int> OnChangeCurrentWave;
        event Action<float> OnChangeRemainTime;
        event Action<int, int> OnChangeAliveEnemy;
    }
}