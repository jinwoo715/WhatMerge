#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Skill.Data;
using UnityEditor;
using UnityEngine;

public static class SkillGraphAssetUtility
{
    public const string DefaultAssetFolder = "Assets/01.Scripts/GamePlay/Skill/Test/SO";
    public const string LayoutFolder = "Assets/01.Scripts/GamePlay/Skill/Editor/GraphLayouts";

    public static ActiveSkillData CreateActiveSkill(string skillName)
    {
        string assetName = SanitizeFileName(skillName);
        if (string.IsNullOrEmpty(assetName))
        {
            assetName = "NewActiveSkill";
        }

        return CreateAsset<ActiveSkillData>(DefaultAssetFolder + "/" + assetName + ".asset");
    }

    public static T CreateAssetForSkill<T>(ActiveSkillData skill, string fallbackName) where T : ScriptableObject
    {
        string prefix = skill != null ? SanitizeFileName(skill.name) : SanitizeFileName(fallbackName);
        if (string.IsNullOrEmpty(prefix))
        {
            prefix = "Skill";
        }

        string suffix = ObjectNames.NicifyVariableName(typeof(T).Name).Replace(" ", string.Empty);
        string folderPath = GetSkillAssetFolder(skill);
        return CreateAsset<T>(folderPath + "/" + prefix + "_" + suffix + ".asset");
    }

    private static string GetSkillAssetFolder(ActiveSkillData skill)
    {
        if (skill != null)
        {
            string skillPath = AssetDatabase.GetAssetPath(skill);
            if (!string.IsNullOrEmpty(skillPath))
            {
                string folderPath = Path.GetDirectoryName(skillPath);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    return folderPath.Replace("\\", "/");
                }
            }
        }

        return DefaultAssetFolder;
    }

    public static T CreateAsset<T>(string assetPath) where T : ScriptableObject
    {
        EnsureFolderExists(assetPath);

        T asset = ScriptableObject.CreateInstance<T>();
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(asset, uniquePath);
        return asset;
    }

    public static void SaveGraph(ActiveSkillData skill)
    {
        foreach (Object referencedObject in GetReferencedObjects(skill))
        {
            MarkDirty(referencedObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static SkillGraphLayoutData LoadGraphLayout(ActiveSkillData skill)
    {
        string assetPath = GetGraphLayoutPath(skill);
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        string json = File.ReadAllText(fullPath);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonUtility.FromJson<SkillGraphLayoutData>(json);
    }

    public static void SaveGraphLayout(ActiveSkillData skill, SkillGraphLayoutData layout)
    {
        string assetPath = GetGraphLayoutPath(skill);
        if (string.IsNullOrEmpty(assetPath) || layout == null)
        {
            return;
        }

        EnsureFolderExists(assetPath);

        string fullPath = Path.GetFullPath(assetPath);
        File.WriteAllText(fullPath, JsonUtility.ToJson(layout, true));
        AssetDatabase.ImportAsset(assetPath);
    }

    public static string GetAssetGuid(Object asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
    }

    private static string GetGraphLayoutPath(ActiveSkillData skill)
    {
        string guid = GetAssetGuid(skill);
        if (string.IsNullOrEmpty(guid))
        {
            return string.Empty;
        }

        return LayoutFolder + "/" + guid + ".json";
    }

    public static ActiveSkillData SaveSkillAs(ActiveSkillData skill)
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Active Skill As", skill.name + "_Copy", "asset", "Choose save location.", DefaultAssetFolder);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        ActiveSkillData copy = ScriptableObject.CreateInstance<ActiveSkillData>();
        EditorUtility.CopySerialized(skill, copy);
        AssetDatabase.CreateAsset(copy, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return copy;
    }

    public static bool ClearGraph(ActiveSkillData skill)
    {
        bool clear = EditorUtility.DisplayDialog("Clear Skill Graph", "Remove all node references from this ActiveSkillSO?", "Clear", "Cancel");
        if (!clear)
        {
            return false;
        }

        Undo.RecordObject(skill, "Clear Skill Graph");
        skill.Execution = null;
        skill.Finder = null;
        skill.Trigger = null;
        MarkDirty(skill);
        return true;
    }

    public static IEnumerable<Object> GetReferencedObjects(ActiveSkillData skill)
    {
        if (skill == null)
        {
            yield break;
        }

        yield return skill;

        if (skill.Trigger != null)
        {
            yield return skill.Trigger;
        }

        if (skill.Finder != null)
        {
            yield return skill.Finder;
        }

        if (skill.Execution == null)
        {
            yield break;
        }

        yield return skill.Execution;

        ExecutionData execution = skill.Execution;
        if (execution.VFX != null)
        {
            yield return execution.VFX;
        }

        if (execution.Effects == null)
        {
            yield break;
        }

        for (int i = 0; i < execution.Effects.Count; i++)
        {
            EffectBase entry = execution.Effects[i];
            if (entry == null)
            {
                continue;
            }

            yield return entry;

            if (entry.VFX != null)
            {
                yield return entry.VFX;
            }

            if (entry is ProjectileSpawnEffect projectileSpawnEffect && projectileSpawnEffect.Projectile != null)
            {
                yield return projectileSpawnEffect.Projectile;
            }

            if (entry is DurationEffect durationEffect && durationEffect.Effects != null)
            {
                for (int j = 0; j < durationEffect.Effects.Count; j++)
                {
                    if (durationEffect.Effects[j] != null)
                    {
                        yield return durationEffect.Effects[j];
                    }
                }
            }

            if (entry is SummonSpawnEffect summonSpawnEffect)
            {
                if (summonSpawnEffect.Move != null)
                {
                    yield return summonSpawnEffect.Move;
                }

                if (summonSpawnEffect.Execution != null)
                {
                    yield return summonSpawnEffect.Execution;
                }

                if (summonSpawnEffect.Execution is SummonOnceExecution onceExecution
                    && onceExecution.Effects != null)
                {
                    for (int j = 0; j < onceExecution.Effects.Count; j++)
                    {
                        if (onceExecution.Effects[j] != null)
                        {
                            yield return onceExecution.Effects[j];
                        }
                    }
                }

                if (summonSpawnEffect.Execution is SummonOnStayExecution stayExecution
                    && stayExecution.Effects != null)
                {
                    for (int j = 0; j < stayExecution.Effects.Count; j++)
                    {
                        if (stayExecution.Effects[j] != null)
                        {
                            yield return stayExecution.Effects[j];
                        }
                    }
                }
            }
        }
    }

    public static void MarkDirty(Object target)
    {
        if (target != null)
        {
            EditorUtility.SetDirty(target);
        }
    }

    public static string RenameAsset(Object asset, string requestedName)
    {
        if (asset == null)
        {
            return string.Empty;
        }

        string assetName = SanitizeFileName(requestedName);
        if (string.IsNullOrEmpty(assetName))
        {
            return asset.name;
        }

        if (asset.name == assetName)
        {
            return asset.name;
        }

        Undo.RecordObject(asset, "Rename Skill Node Asset");

        string path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(path))
        {
            string folderPath = Path.GetDirectoryName(path);
            string extension = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(folderPath))
            {
                folderPath = folderPath.Replace("\\", "/");
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + extension);
                assetName = Path.GetFileNameWithoutExtension(uniquePath);
            }

            string error = AssetDatabase.RenameAsset(path, assetName);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
                return asset.name;
            }
        }
        else
        {
            asset.name = assetName;
        }

        MarkDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset.name;
    }

    private static void EnsureFolderExists(string assetPath)
    {
        string folderPath = Path.GetDirectoryName(assetPath);
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        folderPath = folderPath.Replace("\\", "/");
        string[] folders = folderPath.Split('/');
        if (folders.Length == 0)
        {
            return;
        }

        string current = folders[0];
        for (int i = 1; i < folders.Length; i++)
        {
            string next = current + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, folders[i]);
            }

            current = next;
        }
    }

    public static string SanitizeFileName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string sanitized = value;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            sanitized = sanitized.Replace(invalidChars[i].ToString(), string.Empty);
        }

        return sanitized.Trim();
    }
}

[System.Serializable]
public sealed class SkillGraphLayoutData
{
    public List<SkillGraphLayoutRect> NodePositions = new List<SkillGraphLayoutRect>();
    public List<SkillGraphLayoutRect> AssetPositions = new List<SkillGraphLayoutRect>();
    public List<string> LooseAssetGuids = new List<string>();
    public Vector3 ViewPosition = Vector3.zero;
    public Vector3 ViewScale = Vector3.one;
}

[System.Serializable]
public sealed class SkillGraphLayoutRect
{
    public string Key;
    public Rect Position;

    public SkillGraphLayoutRect()
    {
    }

    public SkillGraphLayoutRect(string key, Rect position)
    {
        Key = key;
        Position = position;
    }
}
#endif
