using Enemies;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[System.Serializable]
public class StageEnemySpriteBundle
{
    public int StageUID;
    public SpriteAtlas SpriteAtlas;
}

[System.Serializable]
public class HeroSpriteSpriteBundle
{
    public int HeroUid;
    public SpriteAtlas SpriteAtlas;
}

public interface ISpriteAtlasRepository
{
    SpriteAtlas GetHeroSpriteAtlas(int uid);
    SpriteAtlas GetStageEnemySpriteAtlas(int uid);
}

public class DataManager : MonoBehaviour, ISkillRepository, ISpriteAtlasRepository, IEnemyDataRepository
{
    [Header("SpriteAtlas")]
    public List<HeroSpriteSpriteBundle> _heroSpriteAtlas;
    public List<StageEnemySpriteBundle> _stageEnemySpriteBundles;

    [Header("TextData")]
    public TextAsset _StageDataText;
    public TextAsset _waveDataText;
    public TextAsset _enemyDataText;
    public TextAsset _activeSkillDataText;
    public TextAsset _atkDataText;
    public TextAsset _heroDataText;

    [Header("Config")]
    public GameConfig _gameConfig;

    private Dictionary<int, SpriteAtlas> _EnemyAtlasByStageUID = new Dictionary<int, SpriteAtlas>();
    private Dictionary<int, SpriteAtlas> _heroAtlasByUID = new Dictionary<int, SpriteAtlas>();
    private Dictionary<int, HeroData> _heroDataByUID = new Dictionary<int, HeroData>();
    private Dictionary<int, EnemyData> _enemyDataByUID = new Dictionary<int, EnemyData>();
    private Dictionary<int, StageData> _stageDataByUID = new Dictionary<int, StageData>();
    private Dictionary<int, ActiveSkillData> _activeSkillDatas = new Dictionary<int, ActiveSkillData>();
    private Dictionary<int, ATKData> _atkDatas = new Dictionary<int, ATKData>();

    public StageSettingConfig StageConfig => _gameConfig.StageConfig;
    public GameEconomyConfig GameEconomy => _gameConfig.GameEconomy;

    public void Init()
    {
        var stageDatas = JsonConvert.DeserializeObject<List<StageData>>(_StageDataText.text);
        for (int i = 0; i < stageDatas.Count; i++)
        {
            StageData data = stageDatas[i];
            data.WaveDatas = new List<WaveData>();
            _stageDataByUID.Add(data.StageUID, data);
        }

        for (int i = 0; i < _heroSpriteAtlas.Count; i++)
        {
            _heroAtlasByUID.Add(_heroSpriteAtlas[i].HeroUid, _heroSpriteAtlas[i].SpriteAtlas);
        }

        var waveDatas = JsonConvert.DeserializeObject<List<WaveData>>(_waveDataText.text);
        for (int i = 0; i < waveDatas.Count; i++)
        {
            WaveData wd = waveDatas[i];
            _stageDataByUID[wd.StageUID].WaveDatas.Add(wd);
        }

        foreach (var stage in _stageDataByUID)
        {
            stage.Value.WaveDatas.Sort((a, b) => a.StartWave.CompareTo(b.StartWave));
        }

        var enemyDatas = JsonConvert.DeserializeObject<List<EnemyData>>(_enemyDataText.text);
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData ed = enemyDatas[i];
            _enemyDataByUID.Add(ed.UID, ed);
        }

        var activeSkillDatas = DeserializeTextData<ActiveSkillData>(_activeSkillDataText.text);
        for (int i = 0; i < activeSkillDatas.Count; i++)
        {
            ActiveSkillData ad = activeSkillDatas[i];
            _activeSkillDatas[ad.UID] = ad;
        }

        var atkDatas = DeserializeTextData<ATKData>(_atkDataText);
        for (int i = 0; i < atkDatas.Count; i++)
        {
            ATKData data = atkDatas[i];
            _atkDatas.Add(data.UID, data);
        }

        var heroDatas = DeserializeTextData<HeroData>(_heroDataText);
        for (int i = 0; i < heroDatas.Count; i++)
        {
            HeroData data = heroDatas[i];
            _heroDataByUID.Add(data.UID, data);
        }

        for (int i = 0; i < _stageEnemySpriteBundles.Count; i++)
        {
            StageEnemySpriteBundle bundle = _stageEnemySpriteBundles[i];
            _EnemyAtlasByStageUID.Add(bundle.StageUID, bundle.SpriteAtlas);
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

    public EnemyData GetEnemyData(int uid)
    {
        if(_enemyDataByUID.TryGetValue(uid, out EnemyData data))
        {
            return data;
        }
        else
        {
            Debug.LogError("Not Exist Enemy UID");
            return default;
        }
    }
    public HeroData GetHeroData(int uid)
    {
        if (_heroDataByUID.TryGetValue(uid, out HeroData data))
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
        if (_stageDataByUID.TryGetValue(uid, out StageData data))
        {
            return data;
        }
        else
        {
            Debug.LogError("Not Exist Stage UID");
            return default;
        }
    }
    public SpriteAtlas GetEnemyAtlas(int uid)
    {
        if (_EnemyAtlasByStageUID.TryGetValue(uid, out SpriteAtlas data))
        {
            return data;
        }
        else
        {
            Debug.LogError($"Not Exist Stage UID : {uid}");
            return default;
        }
    }

    public ActiveSkillData GetActiveSkillData(int uid)
    {
        return _activeSkillDatas[uid];
    }

    public ATKData GetATKData(int uid)
    {
        return _atkDatas[uid];
    }

    public SpriteAtlas GetHeroSpriteAtlas(int uid)
    {
        if (_heroAtlasByUID.TryGetValue(uid, out SpriteAtlas data))
        {
            return data;
        }
        else
        {
            Debug.LogError($"Not Exist Stage UID : {uid}");
            return default;
        }
    }

    public SpriteAtlas GetStageEnemySpriteAtlas(int uid)
    {
        if (_EnemyAtlasByStageUID.TryGetValue(uid, out SpriteAtlas data))
        {
            return data;
        }
        else
        {
            Debug.LogError($"Not Exist Stage UID : {uid}");
            return default;
        }
    }

    public EnemyData GetData(int uid)
    {
        if(_enemyDataByUID.TryGetValue(uid, out EnemyData data))
        {
            return data;
        }
        else
        {
            Debug.LogError($"Not Exist Enemy Data : {uid}");
            return default;
        }
    }
}
