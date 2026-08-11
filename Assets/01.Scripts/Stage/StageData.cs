using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Stage
{
    [CreateAssetMenu(fileName = "Stage", menuName = "Stage/Stage", order = 0)]
    public class StageData : ScriptableObject
    {
        public int UID;
        public string Name;

        [TextArea]
        public string Description;

        [Min(0f)]
        public float NormalWaveDuration;

        [Min(0f)]
        public float BossWaveDuration;

        [Min(1)]
        public int MaxEnemyCount;

        [Min(1)]
        public int WaveCount;

        public List<WaveData> Waves = new();
        public MimicChallengeData MimicChallenge = new();

        public bool TryGetWave(int waveIndex, out WaveData waveData)
        {
            for (int i = 0; i < Waves.Count; i++)
            {
                WaveData candidate = Waves[i];
                if (candidate != null && candidate.WaveIndex == waveIndex)
                {
                    waveData = candidate;
                    return true;
                }
            }

            waveData = null;
            return false;
        }

        public float GetWaveDuration(WaveType waveType)
        {
            return waveType switch
            {
                WaveType.Normal => NormalWaveDuration,
                WaveType.Boss => BossWaveDuration,
                _ => throw new ArgumentOutOfRangeException(nameof(waveType), waveType, null)
            };
        }

        public void ValidateOrThrow()
        {
            if (UID <= 0)
                throw new InvalidOperationException("Stage UID must be greater than zero.");

            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException($"Stage {UID} has no display name.");

            if (NormalWaveDuration <= 0f)
                throw new InvalidOperationException($"Stage {UID} normal wave duration must be greater than zero.");

            if (BossWaveDuration <= 0f)
                throw new InvalidOperationException($"Stage {UID} boss wave duration must be greater than zero.");

            if (MaxEnemyCount <= 0)
                throw new InvalidOperationException($"Stage {UID} max enemy count must be greater than zero.");

            if (WaveCount <= 0)
                throw new InvalidOperationException($"Stage {UID} wave count must be greater than zero.");

            ValidateWaves();
            MimicChallenge?.ValidateOrThrow(UID);
        }

        private void ValidateWaves()
        {
            if (Waves == null || Waves.Count == 0)
                throw new InvalidOperationException($"Stage {UID} has no wave data.");

            bool[] coveredWaves = new bool[WaveCount];

            for (int i = 0; i < Waves.Count; i++)
            {
                WaveData wave = Waves[i]
                    ?? throw new InvalidOperationException($"Stage {UID} wave data at index {i} is null.");

                wave.ValidateOrThrow(UID, WaveCount);

                if (coveredWaves[wave.WaveIndex])
                    throw new InvalidOperationException($"Stage {UID} wave {wave.WaveIndex} is configured more than once.");

                coveredWaves[wave.WaveIndex] = true;
            }

            for (int waveIndex = 0; waveIndex < WaveCount; waveIndex++)
            {
                if (!coveredWaves[waveIndex])
                    throw new InvalidOperationException($"Stage {UID} wave {waveIndex} has no data.");
            }
        }
    }

    [Serializable]
    public class WaveData
    {
        public int WaveIndex;
        public WaveType WaveType;
        public List<EnemySpawnData> SpawnDatas = new();

        public void ValidateOrThrow(int stageUID, int waveCount)
        {
            if (WaveIndex < 0 || WaveIndex >= waveCount)
            {
                throw new InvalidOperationException(
                    $"Stage {stageUID} has an invalid wave index: {WaveIndex}.");
            }

            if (SpawnDatas == null || SpawnDatas.Count == 0)
                throw new InvalidOperationException($"Stage {stageUID} wave {WaveIndex} has no spawn data.");

            for (int i = 0; i < SpawnDatas.Count; i++)
            {
                EnemySpawnData spawnData = SpawnDatas[i]
                    ?? throw new InvalidOperationException(
                        $"Stage {stageUID} wave {WaveIndex} spawn data at index {i} is null.");

                spawnData.ValidateOrThrow(stageUID, WaveIndex);
            }
        }
    }

    [Serializable]
    public class EnemySpawnData
    {
        public int EnemyUID;
        public float StartDelay;
        public int SpawnCount;
        public float SpawnInterval;

        public EnemySpawnData()
        {
        }

        public EnemySpawnData(int uid, int count, float delay, float interval)
        {
            EnemyUID = uid;
            StartDelay = delay;
            SpawnCount = count;
            SpawnInterval = interval;
        }

        public void ValidateOrThrow(int stageUID, int waveIndex)
        {
            if (EnemyUID <= 0)
                throw new InvalidOperationException($"Stage {stageUID} wave {waveIndex} has an invalid enemy UID.");

            if (StartDelay < 0f)
                throw new InvalidOperationException($"Stage {stageUID} wave {waveIndex} has a negative start delay.");

            if (SpawnCount <= 0)
                throw new InvalidOperationException($"Stage {stageUID} wave {waveIndex} spawn count must be greater than zero.");

            if (SpawnInterval < 0f)
                throw new InvalidOperationException($"Stage {stageUID} wave {waveIndex} has a negative spawn interval.");
        }
    }

    [Serializable]
    public class MimicChallengeData
    {
        [Min(0f)]
        public float Cooldown;

        [Min(0f)]
        public float TimeLimit;

        [Min(0)]
        public int BonusBattleCurrency;

        public List<MimicEntryData> Entries = new();

        public bool IsEnabled => Entries != null && Entries.Count > 0;

        public void ValidateOrThrow(int stageUID)
        {
            if (!IsEnabled)
                return;

            if (Cooldown <= 0f)
                throw new InvalidOperationException($"Stage {stageUID} mimic cooldown must be greater than zero.");

            if (TimeLimit <= 0f)
                throw new InvalidOperationException($"Stage {stageUID} mimic time limit must be greater than zero.");

            if (BonusBattleCurrency < 0)
                throw new InvalidOperationException($"Stage {stageUID} mimic bonus currency cannot be negative.");

            for (int i = 0; i < Entries.Count; i++)
            {
                MimicEntryData entry = Entries[i]
                    ?? throw new InvalidOperationException($"Stage {stageUID} mimic entry at index {i} is null.");

                if (entry.EnemyUID <= 0)
                    throw new InvalidOperationException($"Stage {stageUID} mimic entry at index {i} has an invalid enemy UID.");
            }
        }
    }

    [Serializable]
    public class MimicEntryData
    {
        public int EnemyUID;

        public MimicEntryData()
        {
        }

        public MimicEntryData(int enemyUID)
        {
            EnemyUID = enemyUID;
        }
    }
}
