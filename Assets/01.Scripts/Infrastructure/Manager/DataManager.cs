using Heros;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;
using WhatMerge.Heros;

public interface IDataProvider
{
    List<MergeData> MergeData {get;}
}

public class DataManager : MonoBehaviour, IEnemyDataRepository, IDataProvider, IHeroInfoRepository
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
    private Dictionary<int, StageData> _stageDatas = new Dictionary<int, StageData>();
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
        var stageDatas = JsonConvert.DeserializeObject<List<StageData>>(_StageDataText.text);
        for (int i = 0; i < stageDatas.Count; i++)
        {
            StageData data = stageDatas[i];
            data.WaveDatas = new List<WaveData>();
            _stageDatas.Add(data.UID, data);
        }

        foreach (var stage in _stageDatas)
        {
            stage.Value.WaveDatas.Sort((a, b) => a.StartWave.CompareTo(b.StartWave));
        }

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
        InitDictionary(_stageDatas, resourcesReader.GetTextAsset("StageData"));
        InitDictionary(_enemyDatas, resourcesReader.GetTextAsset("EnemyData"));

        var mergeData = resourcesReader.GetTextAsset("MergeData");
        _mergeDatas = DeserializeTextData<MergeData>(mergeData);

        var wave = resourcesReader.GetTextAsset("WaveData");
        var waveDatas = JsonConvert.DeserializeObject<List<WaveData>>(wave.text);

        Debug.Log($"{wave}, {waveDatas}");
        for (int i = 0; i < waveDatas.Count; i++)
        {
            WaveData wd = waveDatas[i];

            if (_stageDatas[wd.StageUID].WaveDatas == null)
                _stageDatas[wd.StageUID].WaveDatas = new List<WaveData>();

            _stageDatas[wd.StageUID].WaveDatas.Add(wd);
        }
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
    public StageData GetStageData(int uid)
    {
        if (_stageDatas.TryGetValue(uid, out StageData data))
        {
            return data;
        }
        else
        {
            Debug.LogError("Not Exist Stage UID");
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

    public HeroSaveData GetHeroSaveData(int uid)
    {
        throw new System.NotImplementedException();
    }
}
