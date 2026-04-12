using UnityEngine;


[CreateAssetMenu(fileName = "GameConfig", menuName = "Config", order = 0)]
public class GameConfig : ScriptableObject
{
    public StageSettingConfig StageConfig;
    public GameEconomyConfig GameEconomy;
}

[System.Serializable]
public class GameEconomyConfig
{
    public int StartMoney;
    public int StartSpawnCost;
    public int IncreaseSpawnCost;
}

[System.Serializable]
public class StageSettingConfig
{
    [Header("Time")]
    public int WaveTime;
    public int BossWaveTime;

    [Header("BossWave")]
    public int BossWavePivot;

    [Header("Enemy")]
    public int MaxEnemy;

    [Header("Player")]
    public int StartMoney;

    [Header("Start Wave")]
    public int StartWaveIndex;
}
