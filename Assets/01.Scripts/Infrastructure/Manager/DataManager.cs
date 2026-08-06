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
    public TextAsset _StageDataText;
    public TextAsset _waveDataText;
    public TextAsset _projectileDataText;
    public TextAsset _summonDataText;
    public TextAsset _buffDataBundleText;
    public TextAsset _buffDataText;
    public TextAsset _mergeDataText;

    [Header("Config")]
    public GameConfig _gameConfig;

    private Dictionary<int, HeroData> _heroDatas = new Dictionary<int, HeroData>();
    private Dictionary<int, EnemyData> _enemyDatas = new Dictionary<int, EnemyData>();
    private Dictionary<int, List<EnemyRewardData>> _enemyRewardsByGroup = new Dictionary<int, List<EnemyRewardData>>();
    private Dictionary<int, ATKData> _atkDatas = new Dictionary<int, ATKData>();

    private Dictionary<int, HeroSaveData> _saveHeroData = new Dictionary<int, HeroSaveData>();

    private List<MergeData> _mergeDatas = new List<MergeData>();

    public StageSettingConfig StageConfig => _gameConfig.StageConfig;
    public GameEconomyConfig GameEconomy => _gameConfig.GameEconomy;
    public PlayerInfoConfig PlayerConfig => _gameConfig.PlayerConfig;

    public List<MergeData> MergeData => _mergeDatas;

    internal HeroSaveData GetSaveHeroData(int heroUid)
    {
        if (_saveHeroData.TryGetValue(heroUid, out var data))
        {
            return data;
        }
        else return new HeroSaveData();
    }

    public void Init()
    {
        _mergeDatas = DeserializeTextData<MergeData>(_mergeDataText);

        foreach (var item in PlayerConfig.HaveHeros)
        {
            _saveHeroData.Add(item.HeroUID, item);
        }
    }

    public void Init(IResourcesReader resourcesReader)
    {
        InitDictionary(_heroDatas, resourcesReader.GetTextAsset("HeroData"));
        InitDictionary(_atkDatas, resourcesReader.GetTextAsset("ATKData"));
        InitDictionary(_enemyDatas, resourcesReader.GetTextAsset("EnemyData"));
        InitRewardDictionary(resourcesReader.GetTextAsset("EnemyRewardData"));

        var mergeData = resourcesReader.GetTextAsset("MergeData");
        _mergeDatas = DeserializeTextData<MergeData>(mergeData);
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

    public ATKData GetATKData(int uid)
    {
        return _atkDatas[uid];
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

    public HeroSaveData GetHeroSaveData(int uid)
    {
        throw new System.NotImplementedException();
    }

    public ItemData GetItemData(int uid)
    {
        throw new System.NotImplementedException();
    }
}
