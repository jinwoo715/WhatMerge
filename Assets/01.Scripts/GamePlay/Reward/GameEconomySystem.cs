using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameGoldService
{
    public int CurrentMony { get; }
    event Action<int> OnChangeMoney;
    void UseMoney(int cost);
    void GainMoney(int cost);
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
