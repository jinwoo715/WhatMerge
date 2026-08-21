#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Skill.Data;
using UnityEditor;
using UnityEngine;
using WhatMerge.Projectiles.Data;
using WhatMerge.Summons.Data;

public static class SkillGraphAssetUtility
{
    public const string DefaultAssetFolder = "Assets/01.Scripts/GamePlay/Skill/Test/SO";
    public const string LayoutFolder = "Assets/01.Scripts/Editor/SkillEditor/Asset";

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
        return SkillGraphReferenceWalker.EnumerateReachableObjects(skill);
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

public readonly struct SkillGraphReference
{
    public Object Child { get; }

    public SkillGraphReference(Object child)
    {
        Child = child;
    }
}

public static class SkillGraphReferenceWalker
{
    public static IEnumerable<Object> EnumerateReachableObjects(Object root)
    {
        var visited = new HashSet<int>();
        foreach (Object referencedObject in EnumerateReachableObjects(root, visited))
        {
            yield return referencedObject;
        }
    }

    public static bool Contains(Object root, Object target)
    {
        if (root == null || target == null)
        {
            return false;
        }

        return Contains(root, target, new HashSet<int>());
    }

    public static bool WouldCreateCycle(Object owner, Object child)
    {
        return owner != null && child != null && Contains(child, owner);
    }

    public static IEnumerable<SkillGraphReference> GetDirectReferences(Object owner)
    {
        if (owner is ActiveSkillData skill)
        {
            yield return new SkillGraphReference(skill.Execution);
            yield return new SkillGraphReference(skill.Finder);
            yield return new SkillGraphReference(skill.Trigger);
            yield break;
        }

        if (owner is ExecutionData execution)
        {
            yield return new SkillGraphReference(execution.ExecutionVFX);
            foreach (SkillGraphReference reference in GetEffectReferences(execution.Effects))
            {
                yield return reference;
            }

            yield break;
        }

        if (owner is ProjectileDataBase projectile)
        {
            foreach (SkillGraphReference reference in GetEffectReferences(projectile.Effects))
            {
                yield return reference;
            }

            yield break;
        }

        if (owner is EffectBase effect)
        {
            yield return new SkillGraphReference(effect.VFX);

            if (effect is ProjectileSpawnEffect projectileSpawn)
            {
                yield return new SkillGraphReference(projectileSpawn.Projectile);
            }

            if (effect is SummonSpawnEffect summonSpawn)
            {
                yield return new SkillGraphReference(summonSpawn.Move);
                yield return new SkillGraphReference(summonSpawn.Execution);
            }

            if (effect is DurationEffect duration)
            {
                foreach (SkillGraphReference reference in GetEffectReferences(duration.Effects))
                {
                    yield return reference;
                }
            }

            if (effect is RangeEffect range)
            {
                foreach (SkillGraphReference reference in GetEffectReferences(range.Effects))
                {
                    yield return reference;
                }
            }

            yield break;
        }

        if (owner is SummonOnceExecution onceExecution)
        {
            foreach (SkillGraphReference reference in GetEffectReferences(onceExecution.Effects))
            {
                yield return reference;
            }

            yield break;
        }

        if (owner is SummonOnStayExecution stayExecution)
        {
            foreach (SkillGraphReference reference in GetEffectReferences(stayExecution.Effects))
            {
                yield return reference;
            }
        }
    }

    private static IEnumerable<SkillGraphReference> GetEffectReferences<T>(IList<T> effects)
        where T : EffectBase
    {
        if (effects == null)
        {
            yield break;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            yield return new SkillGraphReference(effects[i]);
        }
    }

    private static IEnumerable<Object> EnumerateReachableObjects(Object current, HashSet<int> visited)
    {
        if (current == null || !visited.Add(current.GetInstanceID()))
        {
            yield break;
        }

        yield return current;

        foreach (SkillGraphReference reference in GetDirectReferences(current))
        {
            foreach (Object referencedObject in EnumerateReachableObjects(reference.Child, visited))
            {
                yield return referencedObject;
            }
        }
    }

    private static bool Contains(Object current, Object target, HashSet<int> path)
    {
        if (current == target)
        {
            return true;
        }

        int instanceId = current.GetInstanceID();
        if (!path.Add(instanceId))
        {
            return false;
        }

        foreach (SkillGraphReference reference in GetDirectReferences(current))
        {
            if (reference.Child != null && Contains(reference.Child, target, path))
            {
                path.Remove(instanceId);
                return true;
            }
        }

        path.Remove(instanceId);
        return false;
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
