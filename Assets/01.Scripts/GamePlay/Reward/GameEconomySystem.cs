using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Combat.Effects;

public interface IGameGoldService
{
    public int CurrentMony { get; }
    event Action<int> OnChangeMoney;
    void UseMoney(int cost);
    void GainMoney(int cost);
}

public class GoldEffectHandler : IEffectHandler
{
    private IGameGoldService _gameGoldService;

    public GoldEffectHandler(IGameGoldService gameGoldService)
    {
        _gameGoldService = gameGoldService;
    }

    public bool CanHandle(EffectBase effect)
    {
        return effect is GoldEffect;
    }

    public void Handle(EffectBase effect, DamageContext damageContext)
    {
        if(effect is GoldEffect goldEffect)
        {
            _gameGoldService.GainMoney(goldEffect.Gold);
        }
    }
}

public class GameEconomySystem : IGameGoldService
{
    private int _currentMoney;
    public int CurrentMony => _currentMoney;
    public event Action<int> OnChangeMoney;


    public void Init(int initGold)
    {
        _currentMoney = initGold;

        OnChangeMoney?.Invoke(_currentMoney);
    }

    public void UseMoney(int cost)
    {
        _currentMoney -= cost;
        _currentMoney = Mathf.Max(_currentMoney, 0);

        OnChangeMoney?.Invoke(_currentMoney);
    }
    public void GainMoney(int cost)
    {
        _currentMoney += cost;

        OnChangeMoney?.Invoke(_currentMoney);
    }
}
