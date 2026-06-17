using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Stage
{
    public interface IStageService
    {
        event Action OnClearAllWave;
        event Action OnExceedEnemyCount;
        event Action OnTimeOut;
        void StartStage();
        void SummonMiddBoss();
    }
}