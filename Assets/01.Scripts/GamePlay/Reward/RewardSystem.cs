using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ECompensationType
{
    Gold,
}

public class RewardData
{
    public ECompensationType CompensationType;
    public int Value;
}

public interface IRewardProvider
{
    RewardData GetRewardData();
}


public class RewardSystem
{
    private IGameGoldService _gameGoldService;

    public void Init(IGameGoldService goldService)
    {
        _gameGoldService = goldService;
    }

    public void OccurRewards(IRewardProvider rewardProvider)
    {
        RewardData data = rewardProvider.GetRewardData();

        switch (data.CompensationType)
        {
            case ECompensationType.Gold:
                _gameGoldService.GainMoney(data.Value);
                break;
            default:
                break;
        }
    }
}
