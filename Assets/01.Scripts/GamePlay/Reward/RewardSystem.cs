using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyRewardType
{
    BattleCurrency,
    PermanentCurrency,
    Item,
}

[Serializable]
public class EnemyRewardData : BaseData
{
    public int RewardGroupUID;
    public EnemyRewardType RewardType;
    public int RewardUID;
    public int Amount;
    public float DropChance;
}

public interface IEnemyRewardRepository
{
    IReadOnlyList<EnemyRewardData> GetRewards(int rewardGroupUID);
}

public interface IRewardProvider
{
    int RewardGroupUID { get; }
}

public class RewardSystem
{
    private IGameGoldService _gameGoldService;
    private IEnemyRewardRepository _rewardRepository;

    public void Init(IGameGoldService goldService, IEnemyRewardRepository rewardRepository)
    {
        _gameGoldService = goldService ?? throw new ArgumentNullException(nameof(goldService));
        _rewardRepository = rewardRepository ?? throw new ArgumentNullException(nameof(rewardRepository));
    }

    public void OccurRewards(IRewardProvider rewardProvider)
    {
        if (rewardProvider == null)
            throw new ArgumentNullException(nameof(rewardProvider));

        IReadOnlyList<EnemyRewardData> rewards = _rewardRepository.GetRewards(rewardProvider.RewardGroupUID);
        for (int i = 0; i < rewards.Count; i++)
        {
            EnemyRewardData reward = rewards[i];
            if (reward.DropChance <= 0f || UnityEngine.Random.value > reward.DropChance)
                continue;

            switch (reward.RewardType)
            {
                case EnemyRewardType.BattleCurrency:
                    _gameGoldService.GainMoney(reward.Amount);
                    break;
                case EnemyRewardType.PermanentCurrency:
                case EnemyRewardType.Item:
                    Debug.LogWarning($"Reward type {reward.RewardType} is not connected to permanent storage yet.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reward.RewardType), reward.RewardType, null);
            }
        }
    }
}
