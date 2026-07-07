using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Stage
{
    [CreateAssetMenu(fileName ="Stage", menuName = "Stage/Stage", order = 0)]
    public class StageData2 : ScriptableObject
    {
        public string Name;
        public float WaveTime;
        public int MaxAcceptableEnemyCount;
        public List<WaveData2> WaveList;
        public List<BossWaveData> BossWave;
    }

    [System.Serializable]
    public class BossWaveData
    {
        public int Wave;
        public int EnemyUID;
    }

    [System.Serializable]
    public class WaveData2
    {
        public int StartWave;
        public int EndWave;

        [Space]
        public float StartDelay;
        public int SpawnCount;
        public float SpawnInterval;

        [Space]
        public int EnemyUID;
    }

    
}
