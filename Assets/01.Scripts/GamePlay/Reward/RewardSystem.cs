using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyRewordType
{
    Gold,
}

public class RewardData
{
    public EnemyRewordType CompensationType;
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
            case EnemyRewordType.Gold:
                _gameGoldService.GainMoney(data.Value);
                break;
            default:
                break;
        }
    }
}
