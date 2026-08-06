using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Stage
{
    [CreateAssetMenu(fileName = "Stage", menuName = "Stage/Stage", order = 0)]
    public class StageData : ScriptableObject
    {
        public string Name;

        [Tooltip("웨이브 시간")]
        public float NomalWaveTime;
        public float BossWaveTime;

        [Tooltip("게임 종료 적 숫자")]
        public int MaxAcceptableEnemyCount;

        public List<WaveData> WaveList = new();
        public MiddleBossData MiddleBossData = new();
        public List<BossWaveData> BossWaves = new();

        public int GetLastWave
        {
            get
            {
                return WaveList.Count > GetLastBossWave ? WaveList.Count : GetLastBossWave;
            }
        }
        public int GetLastBossWave
        {
            get
            {
                int lastWave = 0;

                for (int i = 0; i < BossWaves.Count; i++)
                {
                    if (BossWaves[i].WaveIndex > lastWave)
                        lastWave = BossWaves[i].WaveIndex;
                }

                return lastWave;
            }
        }
        public WaveData GetWave(int index)
        {
            return WaveList[index - 1];
        }
        public bool TryBossWave(int index, out BossWaveData bossData)
        {
            foreach (var bossWave in BossWaves)
            {
                if (bossWave.WaveIndex == index)
                {
                    bossData = bossWave;
                    return true;
                }
            }

            bossData = default;
            return false;
        }
        public bool TryNomalWave(int index, out WaveData waveData)
        {
            foreach (var wave in WaveList)
            {
                if (wave.WaveIndex == index)
                {
                    waveData = wave;
                    return true;
                }
            }

            waveData = default;
            return false;
        }
        public bool TryMidBoss(int index, out MidBossData midBossData)
        {
            //foreach (var data in MiddleBossDatas)
            //{
            //    if (data.UnlockWave == index)
            //    {
            //        midBossData = data;
            //        return true;
            //    }
            //}

            midBossData = default;
            return false;
        }
    }

    [System.Serializable]
    public class WaveData
    {
        public int WaveIndex;
        public List<EnemySpawnData> SpawnDatas;
    }

    [System.Serializable]
    public class EnemySpawnData
    {
        public int EnemyUID;
        public float StartDelay;
        public int SpawnCount;
        public float SpawnInterval;

        public EnemySpawnData(int uid, int count, float delay, float interval)
        {
            EnemyUID = uid;
            StartDelay = delay;
            SpawnCount = count;
            SpawnInterval = interval;
        }
    }

    public enum EEnemyType
    {
        Nomal,
        MiddleBoss,
        Boss
    }

    [System.Serializable]
    public class MiddleBossData
    {
        public float CoolTime;
        public float TimeLimit;
        public int RewardAmount;

        public List<MidBossData> MidBossDatas;
    }

    [System.Serializable]
    public class MidBossData 
    {
        public Sprite IconSprite;
        public int MidBossUID;
    }

    [System.Serializable]
    public class BossWaveData
    {
        public int WaveIndex;
        public int BossUID;
    }
}
