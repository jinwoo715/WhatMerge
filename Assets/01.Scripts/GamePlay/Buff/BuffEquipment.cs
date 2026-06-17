using Skill;
using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Heros;
public class BuffPayload
{
    public IHeroStatModifier StatModifier { get; private set; }
    public BuffEffect BuffData { get; private set; }

    public BuffPayload(IHeroStatModifier statModifier, BuffEffect buffData)
    {
        StatModifier = statModifier;
        BuffData = buffData;
    }
}

public class BuffEquipment
{
    public Action<BuffEquipment> OnEndBuff;

    public IEnumerator CoApplyBuff(BuffPayload buffPayload)
    {
        var buff = buffPayload.BuffData.BuffData;
        var duration = buffPayload.BuffData.Duration;

        buffPayload.StatModifier.AddMultiplier(buff.BuffType, buff.IncreaseRatio);
        yield return new WaitForSeconds(duration);

        buffPayload.StatModifier.AddMultiplier(buff.BuffType, -buff.IncreaseRatio);
        OnEndBuff?.Invoke(this);
    }

    //private void ApplyBuff(List<BuffData> buffDatas, IStatModifier target)
    //{
    //    for (int i = 0; i < buffDatas.Count; i++)
    //    {
    //        target.ModifyStat(buffDatas[i].BuffValue);
    //    }
    //}

    //private void RevertBuff(List<BuffData> buffDatas, IStatModifier target)
    //{
    //    for (int i = 0; i < buffDatas.Count; i++)
    //    {
    //        target.ModifyStat(-buffDatas[i].BuffValue);
    //    }
    //}
}
