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
    public BuffData BuffData { get; private set; }

    public BuffPayload(IHeroStatModifier statModifier, BuffData buffData)
    {
        StatModifier = statModifier;
        BuffData = buffData;
    }
}

public class BuffEquipment
{
    private BuffPayload _payload;
    public Coroutine Coroutine;
    public event Action<BuffEquipment> OnEndBuff;

    public void AppplyBuff(BuffPayload payload, Coroutine coroutine) 
    {
        Coroutine = coroutine;
        _payload = payload;

        var buff = _payload.BuffData;
        var modifier = _payload.StatModifier;

        modifier.AddMultiplier(buff.BuffType, buff.IncreaseRatio);
    }

    public void ReleaseBuff() 
    {
        var buff = _payload.BuffData;
        var modifier = _payload.StatModifier;

        modifier.AddMultiplier(buff.BuffType, -buff.IncreaseRatio);

        OnEndBuff?.Invoke(this);

        Coroutine = null;
        _payload = null;
    }
}
