using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Stage
{
    public interface IStageService
    {
        event Action OnStageClear;
        event Action OnStageFail;

        void StartStage();
        void SummonMiddBoss();
    }
}