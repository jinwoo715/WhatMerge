using Enemies;
using Heros;
using Newtonsoft.Json;
using Skill;
using Skill.Projectile;
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

public interface IDataProvider
{
    ProjectileData GetProjecTileData(int uid);
    
    List<MergeData> MergeData {get;}
}

public class DataManager : MonoBehaviour, ISpriteAtlasRepository, IEnemyDataRepository, IDataProvider, IHeroInfoRepository
{
    [Header("SpriteAtlas")]
    public List<HeroSpriteSpriteBundle> _heroSpriteAtlas;
    public List<StageEnemySpriteBundle> _stageEnemySpriteBundles;
    public SpriteAtlas _effectAtlas;
    public SpriteAtlas _projectileAtlas;

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

    private Dictionary<int, SpriteAtlas> _EnemyAtlasByStageUID = new Dictionary<int, SpriteAtlas>();
    private Dictionary<int, SpriteAtlas> _heroAtlasByUID = new Dictionary<int, SpriteAtlas>();
    private Dictionary<int, HeroData> _heroDatas = new Dictionary<int, HeroData>();
    private Dictionary<int, EnemyData> _enemyDatas = new Dictionary<int, EnemyData>();
    private Dictionary<int, StageData> _stageDatas = new Dictionary<int, StageData>();
    private Dictionary<int, ATKData> _atkDatas = new Dictionary<int, ATKData>();
    private Dictionary<int, ProjectileData> _projectileDatas = new Dictionary<int, ProjectileData>();

    private Dictionary<string, SpriteAtlas> _spriteAtlas = new Dictionary<string, SpriteAtlas>();

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
        _spriteAtlas.Add(_effectAtlas.name, _effectAtlas);

        var stageDatas = JsonConvert.DeserializeObject<List<StageData>>(_StageDataText.text);
        for (int i = 0; i < stageDatas.Count; i++)
        {
            StageData data = stageDatas[i];
            data.WaveDatas = new List<WaveData>();
            _stageDatas.Add(data.UID, data);
        }

        for (int i = 0; i < _heroSpriteAtlas.Count; i++)
        {
            _heroAtlasByUID.Add(_heroSpriteAtlas[i].HeroUid, _heroSpriteAtlas[i].SpriteAtlas);
        }

        

        foreach (var stage in _stageDatas)
        {
            stage.Value.WaveDatas.Sort((a, b) => a.StartWave.CompareTo(b.StartWave));
        }

        var projectileDatas = DeserializeTextData<ProjectileData>(_projectileDataText);
        for (int i = 0; i < projectileDatas.Count; i++)
        {
            ProjectileData data = projectileDatas[i];
            _projectileDatas.Add(data.ProjectileUID, data);
        }

        _mergeDatas = DeserializeTextData<MergeData>(_mergeDataText);

        for (int i = 0; i < _stageEnemySpriteBundles.Count; i++)
        {
            StageEnemySpriteBundle bundle = _stageEnemySpriteBundles[i];
            _EnemyAtlasByStageUID.Add(bundle.StageUID, bundle.SpriteAtlas);
        }

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
    public SpriteAtlas GetSpriteAtlas(string name)
    {
        if(_spriteAtlas.TryGetValue(name, out SpriteAtlas atlas))
            return atlas;

        Debug.LogError($"Not Exist Atlas By : {name}");
        return default;
    }

    public ProjectileData GetProjecTileData(int uid)
    {
        if (_projectileDatas.TryGetValue(uid, out ProjectileData data))
            return data;

        Debug.LogError($"Not Exist Data By : {uid}");
        return default;
    }

    public HeroSaveData GetHeroSaveData(int uid)
    {
        throw new System.NotImplementedException();
    }
}
