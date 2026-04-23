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
    SpriteAtlas GetSpriteAtlas(string name);
}

public class DataManager : MonoBehaviour, ISkillRepository, ISpriteAtlasRepository, IEnemyDataRepository, IDataProvider
{
    [Header("SpriteAtlas")]
    public List<HeroSpriteSpriteBundle> _heroSpriteAtlas;
    public List<StageEnemySpriteBundle> _stageEnemySpriteBundles;
    public SpriteAtlas _effectAtlas;
    public SpriteAtlas _projectileAtlas;

    [Header("TextData")]
    public TextAsset _StageDataText;
    public TextAsset _waveDataText;
    public TextAsset _enemyDataText;
    public TextAsset _activeSkillDataText;
    public TextAsset _atkDataText;
    public TextAsset _heroDataText;
    public TextAsset _projectileDataText;
    public TextAsset _summonDataText;
    public TextAsset _buffDataBundleText;
    public TextAsset _buffDataText;

    [Header("Config")]
    public GameConfig _gameConfig;

    private Dictionary<int, SpriteAtlas> _EnemyAtlasByStageUID = new Dictionary<int, SpriteAtlas>();
    private Dictionary<int, SpriteAtlas> _heroAtlasByUID = new Dictionary<int, SpriteAtlas>();
    private Dictionary<int, HeroData> _heroDataByUID = new Dictionary<int, HeroData>();
    private Dictionary<int, EnemyData> _enemyDataByUID = new Dictionary<int, EnemyData>();
    private Dictionary<int, StageData> _stageDataByUID = new Dictionary<int, StageData>();
    private Dictionary<int, ActiveSkillData> _activeSkillDatas = new Dictionary<int, ActiveSkillData>();
    private Dictionary<int, ATKData> _atkDatas = new Dictionary<int, ATKData>();
    private Dictionary<int, ProjectileData> _projectileDatas = new Dictionary<int, ProjectileData>();
    private Dictionary<int, SummonData> _summonDatas = new Dictionary<int, SummonData>();

    private Dictionary<int, BuffDataBundle> _buffDataBundle = new Dictionary<int, BuffDataBundle>();
    private Dictionary<int, BuffData> _buffData = new Dictionary<int, BuffData>();

    private Dictionary<string, SpriteAtlas> _spriteAtlas = new Dictionary<string, SpriteAtlas>();

    public StageSettingConfig StageConfig => _gameConfig.StageConfig;
    public GameEconomyConfig GameEconomy => _gameConfig.GameEconomy;

    public void Init()
    {
        _spriteAtlas.Add(_effectAtlas.name, _effectAtlas);

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

        var projectileDatas = DeserializeTextData<ProjectileData>(_projectileDataText);
        for (int i = 0; i < projectileDatas.Count; i++)
        {
            ProjectileData data = projectileDatas[i];
            _projectileDatas.Add(data.ProjectileUID, data);
        }

        var summonDatas = DeserializeTextData<SummonData>(_summonDataText);
        for (int i = 0; i < summonDatas.Count; i++)
        {
            SummonData data = summonDatas[i];
            _summonDatas.Add(data.UID, data);
        }

        InitDictionary(_buffDataBundle, _buffDataBundleText);
        InitDictionary(_buffData, _buffDataText);

        for (int i = 0; i < _stageEnemySpriteBundles.Count; i++)
        {
            StageEnemySpriteBundle bundle = _stageEnemySpriteBundles[i];
            _EnemyAtlasByStageUID.Add(bundle.StageUID, bundle.SpriteAtlas);
        }
    }

    private void InitDictionary<T>(Dictionary<int, T> dic, TextAsset text) where T : Data
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

    public SpriteAtlas GetSpriteAtlas(string name)
    {
        if(_spriteAtlas.TryGetValue(name, out SpriteAtlas atlas))
            return atlas;

        Debug.LogError($"Not Exist Atlas By : {name}");
        return default;
    }

    public SpriteAtlas GetProjectileAtlas()
    {
        return _projectileAtlas;
    }

    public ProjectileData GetProjecTileData(int uid)
    {
        if (_projectileDatas.TryGetValue(uid, out ProjectileData data))
            return data;

        Debug.LogError($"Not Exist Data By : {uid}");
        return default;
    }

    public SummonData GetSummonData(int uid)
    {
        if (_summonDatas.TryGetValue(uid, out SummonData data))
            return data;

        Debug.LogError($"Not Exist Data By : {uid}");
        return default;
    }

    public List<BuffData> GetBuffDatas(int uid)
    {
        var bundleData = _buffDataBundle[uid];

        List<BuffData> datas = new List<BuffData>();

        if (bundleData.FirstBuff != 0)
            datas.Add(_buffData[bundleData.FirstBuff]);

        if (bundleData.SecondBuff != 0)
            datas.Add(_buffData[bundleData.SecondBuff]);

        if (bundleData.ThirdBuff != 0)
            datas.Add(_buffData[bundleData.ThirdBuff]);

        return datas;
    }
}
