using UnityEngine;


[CreateAssetMenu(fileName = "GameConfig", menuName = "Config", order = 0)]
public class GameConfig : ScriptableObject
{
    public StageConfig StageConfig;
}

[System.Serializable]
public class StageConfig
{
    public float WaveTime;
    public int FailEnemyCount;
}
