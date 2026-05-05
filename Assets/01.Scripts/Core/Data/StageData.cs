using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StageData : Data
{
    public string StageName;
    public string StageDescription;
    public List<WaveData> WaveDatas;
}

[System.Serializable]
public class WaveData
{
    public int StageUID;
    public int StartWave;
    public int EndWave;
    public float StartDelay;
    public int EnemyUID;
    public int SpawnCount;
    public float SpawnInterval;
}

[System.Serializable]
public class WaveDataBundle
{
    public List<WaveData> Waves;
}

public class StageReward
{
    public int RewardUID;
}
