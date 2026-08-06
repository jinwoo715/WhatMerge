using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Stage
{
    public interface IStageService
    {
        event Action OnStageClear;
        event Action OnStageFail;
        event Action<MidBossData, int> OnShowMiddleBossSpawnButton;
        event Action OnHideMiddleBossSpawnButton;

        void SummonMiddBoss();
    }
}
