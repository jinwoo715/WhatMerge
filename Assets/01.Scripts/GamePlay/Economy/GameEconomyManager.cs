using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEconomyService
{
    public int CurrentMony { get; }

    event Action<int> OnChangeMoney;

    void UseMoney(int cost);
}

public class GameEconomyManager : IEconomyService
{
    private int _currentMoney;
    private GameEconomyConfig _gameEconomy;

    public int CurrentMony => _currentMoney;


    public event Action<int> OnChangeMoney;

    public void Init(GameEconomyConfig gameEconomy)
    {
        _currentMoney = gameEconomy.StartMoney;
        _gameEconomy = gameEconomy;

        OnChangeMoney?.Invoke(_currentMoney);
    }

    public void UseMoney(int cost)
    {
        _currentMoney -= cost;

        _currentMoney = Mathf.Max(_currentMoney, 0);
    }
}
