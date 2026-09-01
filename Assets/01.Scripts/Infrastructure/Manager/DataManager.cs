using Heros;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;
using WhatMerge.Heros;

public interface IDataProvider
{
    List<MergeData> MergeData {get;}
    List<MythicMergeData> MythicMergeData { get; }
}

public class ItemData
{
    public int UID;
    public string Name;
    public string Description;
}

public interface IItemRepository
{
    ItemData GetItemData(int uid);
}

public class DataManager : MonoBehaviour, IEnemyDataRepository, IEnemyRewardRepository, IDataProvider, IHeroInfoRepository, IItemRepository
{
    [Header("TextData")]
    public TextAsset _mergeDataText;

    [Header("Config")]
    public GameConfig _gameConfig;

    private Dictionary<int, HeroData> _heroDatas = new Dictionary<int, HeroData>();
    private Dictionary<int, EnemyData> _enemyDatas = new Dictionary<int, EnemyData>();
    private Dictionary<int, List<EnemyRewardData>> _enemyRewardsByGroup = new Dictionary<int, List<EnemyRewardData>>();

    private Dictionary<int, HeroSaveData> _saveHeroData = new Dictionary<int, HeroSaveData>();

    private List<MergeData> _mergeDatas = new List<MergeData>();
    private List<MythicMergeData> _mythicMergeDatas = new List<MythicMergeData>();

    public GameEconomyConfig GameEconomy => _gameConfig.GameEconomy;
    public PlayerInfoConfig PlayerConfig => _gameConfig.PlayerConfig;
    public HeroProgressionConfig HeroProgression => _gameConfig.HeroProgression;

    public List<MergeData> MergeData => _mergeDatas;
    public List<MythicMergeData> MythicMergeData => _mythicMergeDatas;

    public void Init(IResourcesReader resourcesReader)
    {
        InitDictionary(_heroDatas, resourcesReader.GetTextAsset("HeroData"));
        InitDictionary(_enemyDatas, resourcesReader.GetTextAsset("EnemyData"));
        InitRewardDictionary(resourcesReader.GetTextAsset("EnemyRewardData"));

        if (HeroProgression == null || HeroProgression.MaxLevel < 1)
            throw new InvalidOperationException("Hero progression MaxLevel must be greater than zero.");
        if (PlayerConfig?.HaveHeros == null)
            throw new InvalidOperationException("Player hero save list is null.");

        foreach (var item in PlayerConfig.HaveHeros)
        {
            if (item == null)
                throw new InvalidOperationException("Player hero save list contains a null entry.");
            if (item.Level < 1 || item.Level > HeroProgression.MaxLevel)
            {
                throw new InvalidOperationException(
                    $"Hero UID {item.HeroUID} level {item.Level} is outside " +
                    $"1-{HeroProgression.MaxLevel}.");
            }
            if (!_heroDatas.ContainsKey(item.HeroUID))
                throw new InvalidOperationException($"Hero save UID {item.HeroUID} has no HeroData.");

            _saveHeroData.Add(item.HeroUID, item);
        }

        var mergeData = resourcesReader.GetTextAsset("MergeData");
        _mergeDatas = DeserializeTextData<MergeData>(mergeData);

        var mythicMergeData = resourcesReader.GetTextAsset("MythicMergeData");
        _mythicMergeDatas = DeserializeTextData<MythicMergeData>(mythicMergeData);
    }

    private void InitDictionary<T>(Dictionary<int, T> dic, TextAsset text) where T : BaseData
    {
        var datas = DeserializeTextData<T>(text);
        for (int i = 0; i < datas.Count; i++)
        {
            T data = datas[i];
            dic.Add(data.UID, data);
        }
    }

    private void InitRewardDictionary(TextAsset text)
    {
        List<EnemyRewardData> rewards = DeserializeTextData<EnemyRewardData>(text);
        for (int i = 0; i < rewards.Count; i++)
        {
            EnemyRewardData reward = rewards[i];
            if (!_enemyRewardsByGroup.TryGetValue(reward.RewardGroupUID, out List<EnemyRewardData> group))
            {
                group = new List<EnemyRewardData>();
                _enemyRewardsByGroup.Add(reward.RewardGroupUID, group);
            }

            group.Add(reward);
        }
    }
    private List<T> DeserializeTextData<T>(TextAsset textAsset)
    {
        return DeserializeTextData<T>(textAsset.text);
    }
    private List<T> DeserializeTextData<T>(string text)
    {
        return JsonConvert.DeserializeObject<List<T>>(text);
    }
 
    public HeroData GetHeroData(int uid)
    {
        if (_heroDatas.TryGetValue(uid, out HeroData data))
        {
            return data;
        }
        else
        {
            Debug.LogError($"Not Exist Hero UID : {uid}");
            return default;
        }
    }

    public EnemyData GetData(int uid)
    {
        if(_enemyDatas.TryGetValue(uid, out EnemyData data))
        {
            return data;
        }
        else
        {
            Debug.LogError($"Not Exist Enemy Data : {uid}");
            return default;
        }
    }

    public IReadOnlyList<EnemyRewardData> GetRewards(int rewardGroupUID)
    {
        return _enemyRewardsByGroup.TryGetValue(rewardGroupUID, out List<EnemyRewardData> rewards)
            ? rewards
            : Array.Empty<EnemyRewardData>();
    }

    public bool TryGetHeroSaveData(int uid, out HeroSaveData data)
    {
        return _saveHeroData.TryGetValue(uid, out data);
    }

    public ItemData GetItemData(int uid)
    {
        throw new System.NotImplementedException();
    }
}
