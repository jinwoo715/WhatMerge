#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using WhatMerge.Enemies;

namespace WhatMerge.Stage.Editor
{
    internal enum StageValidationSeverity
    {
        Success,
        Info,
        Warning,
        Error
    }

    internal readonly struct StageValidationMessage
    {
        public StageValidationMessage(StageValidationSeverity severity, string text)
        {
            Severity = severity;
            Text = text;
        }

        public StageValidationSeverity Severity { get; }
        public string Text { get; }
    }

    internal sealed class StageEditorCatalog
    {
        private readonly List<EnemyData> _enemies = new List<EnemyData>();
        private readonly Dictionary<int, EnemyData> _enemyByUID = new Dictionary<int, EnemyData>();
        private readonly Dictionary<EnemyType, List<EnemyData>> _enemiesByType =
            new Dictionary<EnemyType, List<EnemyData>>();
        private readonly Dictionary<int, List<EnemyRewardData>> _rewardsByGroup =
            new Dictionary<int, List<EnemyRewardData>>();

        public string LoadError { get; private set; }
        public bool IsLoaded => string.IsNullOrEmpty(LoadError);

        public void Reload()
        {
            _enemies.Clear();
            _enemyByUID.Clear();
            _enemiesByType.Clear();
            _rewardsByGroup.Clear();
            LoadError = null;

            try
            {
                IList enemyRows = global::DataTransformer.ParseExcelDataToList(typeof(EnemyData), "EnemyData");
                IList rewardRows = global::DataTransformer.ParseExcelDataToList(typeof(EnemyRewardData), "EnemyRewardData");

                if (enemyRows == null || rewardRows == null)
                    throw new InvalidOperationException("Enemy data CSV parsing failed.");

                for (int i = 0; i < enemyRows.Count; i++)
                {
                    EnemyData enemy = (EnemyData)enemyRows[i];
                    if (!_enemyByUID.TryAdd(enemy.UID, enemy))
                        throw new InvalidOperationException($"Enemy UID {enemy.UID} is duplicated.");

                    _enemies.Add(enemy);
                    if (!_enemiesByType.TryGetValue(enemy.EnemyType, out List<EnemyData> typedEnemies))
                    {
                        typedEnemies = new List<EnemyData>();
                        _enemiesByType.Add(enemy.EnemyType, typedEnemies);
                    }

                    typedEnemies.Add(enemy);
                }

                _enemies.Sort((left, right) => left.UID.CompareTo(right.UID));
                foreach (List<EnemyData> typedEnemies in _enemiesByType.Values)
                    typedEnemies.Sort((left, right) => left.UID.CompareTo(right.UID));

                for (int i = 0; i < rewardRows.Count; i++)
                {
                    EnemyRewardData reward = (EnemyRewardData)rewardRows[i];
                    if (!_rewardsByGroup.TryGetValue(reward.RewardGroupUID, out List<EnemyRewardData> rewards))
                    {
                        rewards = new List<EnemyRewardData>();
                        _rewardsByGroup.Add(reward.RewardGroupUID, rewards);
                    }

                    rewards.Add(reward);
                }
            }
            catch (Exception exception)
            {
                LoadError = exception.Message;
            }
        }

        public bool TryGetEnemy(int uid, out EnemyData enemy)
        {
            return _enemyByUID.TryGetValue(uid, out enemy);
        }

        public IReadOnlyList<EnemyData> GetEnemies(EnemyType enemyType)
        {
            return _enemiesByType.TryGetValue(enemyType, out List<EnemyData> enemies)
                ? enemies
                : Array.Empty<EnemyData>();
        }

        public string GetEnemyLabel(int uid)
        {
            return TryGetEnemy(uid, out EnemyData enemy)
                ? $"{enemy.UID} - {enemy.Name} - {enemy.EnemyType}"
                : $"{uid} - Missing Enemy";
        }

        public string GetRewardSummary(int enemyUID)
        {
            if (!TryGetEnemy(enemyUID, out EnemyData enemy))
                return "Reward unavailable";

            if (!_rewardsByGroup.TryGetValue(enemy.RewardGroupUID, out List<EnemyRewardData> rewards)
                || rewards.Count == 0)
            {
                return "No base reward";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < rewards.Count; i++)
            {
                EnemyRewardData reward = rewards[i];
                parts.Add($"{reward.RewardType} {reward.Amount} ({reward.DropChance:P0})");
            }

            return string.Join(", ", parts);
        }
    }

    internal sealed class StageEditorSpriteResolver
    {
        private readonly Dictionary<int, List<Sprite>> _spritesByStage = new Dictionary<int, List<Sprite>>();

        public void Clear()
        {
            _spritesByStage.Clear();
        }

        public Sprite GetPreviewSprite(int stageUID, string spriteKey)
        {
            if (stageUID <= 0 || string.IsNullOrWhiteSpace(spriteKey))
                return null;

            if (!_spritesByStage.TryGetValue(stageUID, out List<Sprite> sprites))
            {
                sprites = LoadStageSprites(stageUID);
                _spritesByStage.Add(stageUID, sprites);
            }

            for (int i = 0; i < sprites.Count; i++)
            {
                string spriteName = sprites[i].name;
                if (spriteName.Equals(spriteKey, StringComparison.Ordinal)
                    || spriteName.StartsWith(spriteKey + "_", StringComparison.Ordinal))
                {
                    return sprites[i];
                }
            }

            return null;
        }

        private static List<Sprite> LoadStageSprites(int stageUID)
        {
            string atlasName = $"Stage{stageUID}";
            string[] guids = AssetDatabase.FindAssets(atlasName);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null)
                    continue;

                Sprite[] spriteArray = new Sprite[atlas.spriteCount];
                atlas.GetSprites(spriteArray);
                List<Sprite> sprites = new List<Sprite>(spriteArray);
                sprites.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
                return sprites;
            }

            return new List<Sprite>();
        }
    }

    internal static class StageDataEditorValidator
    {
        public static List<StageValidationMessage> Validate(StageData stage, StageEditorCatalog catalog)
        {
            List<StageValidationMessage> messages = new List<StageValidationMessage>();
            if (stage == null)
            {
                messages.Add(Error("Select a StageData asset."));
                return messages;
            }

            ValidateStageSettings(stage, messages);
            ValidateWaves(stage, catalog, messages);
            ValidateMiddleBoss(stage, catalog, messages);

            if (!catalog.IsLoaded)
                messages.Add(Warning($"Enemy catalog unavailable: {catalog.LoadError}"));

            return messages;
        }

        public static bool HasErrors(IReadOnlyList<StageValidationMessage> messages)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Severity == StageValidationSeverity.Error)
                    return true;
            }

            return false;
        }

        private static void ValidateStageSettings(StageData stage, ICollection<StageValidationMessage> messages)
        {
            if (stage.UID <= 0)
                messages.Add(Error("Stage UID must be greater than zero."));

            if (string.IsNullOrWhiteSpace(stage.Name))
                messages.Add(Error("Stage display name is empty."));

            if (stage.NormalWaveDuration <= 0f)
                messages.Add(Error("Normal wave duration must be greater than zero."));

            if (stage.BossWaveDuration <= 0f)
                messages.Add(Error("Boss wave duration must be greater than zero."));

            if (stage.MaxEnemyCount <= 0)
                messages.Add(Error("Max enemy count must be greater than zero."));

            if (stage.WaveCount <= 0)
                messages.Add(Error("Wave count must be greater than zero."));
        }

        private static void ValidateWaves(
            StageData stage,
            StageEditorCatalog catalog,
            ICollection<StageValidationMessage> messages)
        {
            if (stage.WaveCount <= 0)
                return;

            int[] coverage = new int[stage.WaveCount + 1];
            bool hasDuplicate = false;
            bool enemyTypesValid = true;
            int configuredWaveCount = 0;

            if (stage.Waves == null || stage.Waves.Count == 0)
            {
                messages.Add(Error("No waves are configured."));
                enemyTypesValid = false;
            }
            else
            {
                for (int i = 0; i < stage.Waves.Count; i++)
                {
                    WaveData wave = stage.Waves[i];
                    if (wave == null)
                    {
                        messages.Add(Error($"Wave at list index {i} is null."));
                        enemyTypesValid = false;
                        continue;
                    }

                    bool validWaveIndex = wave.WaveIndex > 0 && wave.WaveIndex <= stage.WaveCount;
                    if (!validWaveIndex)
                    {
                        messages.Add(Error($"Invalid wave index {wave.WaveIndex}."));
                    }
                    else
                    {
                        coverage[wave.WaveIndex]++;
                        if (coverage[wave.WaveIndex] > 1)
                            hasDuplicate = true;
                    }

                    if (wave.SpawnDatas == null || wave.SpawnDatas.Count == 0)
                    {
                        messages.Add(Error($"Wave {wave.WaveIndex} has no spawn entries."));
                        enemyTypesValid = false;
                        continue;
                    }

                    EnemyType expectedType = wave.WaveType == WaveType.Boss
                        ? EnemyType.Boss
                        : EnemyType.Normal;

                    for (int spawnIndex = 0; spawnIndex < wave.SpawnDatas.Count; spawnIndex++)
                    {
                        EnemySpawnData spawn = wave.SpawnDatas[spawnIndex];
                        if (spawn == null)
                        {
                            messages.Add(Error($"Wave {wave.WaveIndex} has a null spawn entry."));
                            enemyTypesValid = false;
                            continue;
                        }

                        if (spawn.SpawnCount <= 0 || spawn.StartDelay < 0f || spawn.SpawnInterval < 0f)
                        {
                            messages.Add(Error($"Wave {wave.WaveIndex} has invalid spawn values."));
                        }

                        if (!catalog.TryGetEnemy(spawn.EnemyUID, out EnemyData enemy))
                        {
                            messages.Add(Error($"Wave {wave.WaveIndex} references missing enemy {spawn.EnemyUID}."));
                            enemyTypesValid = false;
                        }
                        else if (enemy.EnemyType != expectedType)
                        {
                            messages.Add(Error(
                                $"Wave {wave.WaveIndex} is {wave.WaveType}, but enemy {enemy.UID} is {enemy.EnemyType}."));
                            enemyTypesValid = false;
                        }
                    }
                }
            }

            for (int waveIndex = 1; waveIndex <= stage.WaveCount; waveIndex++)
            {
                if (coverage[waveIndex] > 0)
                    configuredWaveCount++;
            }

            if (configuredWaveCount == stage.WaveCount)
                messages.Add(Success($"{configuredWaveCount} / {stage.WaveCount} waves configured"));
            else
                messages.Add(Error($"{stage.WaveCount - configuredWaveCount} wave(s) have no configuration."));

            messages.Add(hasDuplicate
                ? Error("Wave indexes are duplicated.")
                : Success("No duplicate wave indexes"));

            if (enemyTypesValid)
                messages.Add(Success("Enemy types valid"));
        }

        private static void ValidateMiddleBoss(
            StageData stage,
            StageEditorCatalog catalog,
            ICollection<StageValidationMessage> messages)
        {
            MiddleBossChallengeData challenge = stage.MiddleBossChallenge;
            if (challenge == null || !challenge.IsEnabled)
            {
                messages.Add(Info("Middle boss not configured"));
                return;
            }

            bool valid = true;
            if (challenge.Cooldown <= 0f)
            {
                messages.Add(Error("Middle boss cooldown must be greater than zero."));
                valid = false;
            }

            if (challenge.TimeLimit <= 0f)
            {
                messages.Add(Error("Middle boss time limit must be greater than zero."));
                valid = false;
            }

            if (challenge.BonusBattleCurrency < 0)
            {
                messages.Add(Error("Middle boss bonus currency cannot be negative."));
                valid = false;
            }

            for (int i = 0; i < challenge.Entries.Count; i++)
            {
                MiddleBossEntryData entry = challenge.Entries[i];
                if (entry == null || !catalog.TryGetEnemy(entry.EnemyUID, out EnemyData enemy))
                {
                    messages.Add(Error($"Middle boss entry {i + 1} references a missing enemy."));
                    valid = false;
                }
                else if (enemy.EnemyType != EnemyType.MiddleBoss)
                {
                    messages.Add(Error($"Enemy {enemy.UID} is {enemy.EnemyType}, not MiddleBoss."));
                    valid = false;
                }
            }

            if (valid)
                messages.Add(Success($"{challenge.Entries.Count} middle boss entry(s) valid"));
        }

        private static StageValidationMessage Success(string text)
        {
            return new StageValidationMessage(StageValidationSeverity.Success, text);
        }

        private static StageValidationMessage Info(string text)
        {
            return new StageValidationMessage(StageValidationSeverity.Info, text);
        }

        private static StageValidationMessage Warning(string text)
        {
            return new StageValidationMessage(StageValidationSeverity.Warning, text);
        }

        private static StageValidationMessage Error(string text)
        {
            return new StageValidationMessage(StageValidationSeverity.Error, text);
        }
    }
}
#endif
