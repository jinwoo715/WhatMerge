#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Skill.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WhatMerge.Heros;

[CustomEditor(typeof(EffectValueEnhanceData))]
public class EffectStatEnhanceDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "TargetStatKey", "AddValue");

        var data = (EffectValueEnhanceData)target;
        EffectBase targetEffect = data.TargetEffect;

        if (targetEffect is IEffectValueModifier modifier)
        {
            var stats = modifier.GetEnhanceableStats();

            if (stats == null || stats.Count == 0)
            {
                EditorGUILayout.HelpBox("선택한 Effect는 강화 가능한 수치가 없습니다.", MessageType.Info);
            }
            else
            {
                string[] labels = stats.Select(x => x.Label).ToArray();
                int currentIndex = 0;

                for (int i = 0; i < stats.Count; i++)
                {
                    if (stats[i].Key == data.TargetStatKey)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                int selectedIndex = EditorGUILayout.Popup("Target Stat", currentIndex, labels);

                data.TargetStatKey = stats[selectedIndex].Key;
                data.AddValue = EditorGUILayout.FloatField("Add Value", data.AddValue);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("선택한 Effect는 강화 가능한 수치가 없습니다.", MessageType.Info);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(SkillSetContainer))]
public class SkillSetContainerEditor : Editor
{
    private const string HeroDataPath = "Assets/06.Data/JSON/HeroData.json";
    private const string GameConfigPath = "Assets/03.Prefabs/Config/GameConfig.asset";

    private readonly List<string> _validationErrors = new List<string>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SkillSetContainer container = (SkillSetContainer)target;
        HeroData heroData = FindHeroData(container.UID);
        GameConfig gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("UID"));
        DrawHeroSummary(heroData);

        SerializedProperty gradeSets = serializedObject.FindProperty("GradeSets");
        EditorGUILayout.PropertyField(gradeSets, true);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = heroData != null;
            if (GUILayout.Button("Rebuild Grade Groups"))
                RebuildGradeGroups(container, heroData);

            GUI.enabled = container.GradeSets != null;
            if (GUILayout.Button("Sort All By Level"))
                SortAllGroups(container);

            GUI.enabled = heroData != null && gameConfig?.HeroProgression != null;
            if (GUILayout.Button("Validate"))
            {
                _validationErrors.Clear();
                _validationErrors.AddRange(
                    SkillSetValidator.Validate(
                        container,
                        heroData,
                        gameConfig.HeroProgression.MaxLevel));
            }

            GUI.enabled = true;
        }

        DrawValidationErrors();
        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawHeroSummary(HeroData heroData)
    {
        if (heroData == null)
        {
            EditorGUILayout.HelpBox(
                "UID에 해당하는 HeroData를 찾을 수 없습니다.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Hero", $"{heroData.Name} (UID {heroData.UID})");
        EditorGUILayout.LabelField("Base Grade", heroData.BaseGrade.ToString());
        EditorGUILayout.LabelField(
            "Reachable Grades",
            $"{heroData.BaseGrade} / {(HeroGrade)((int)heroData.BaseGrade + 1)} / " +
            $"{(HeroGrade)((int)heroData.BaseGrade + 2)}");
    }

    private void DrawValidationErrors()
    {
        if (_validationErrors.Count == 0)
            return;

        EditorGUILayout.Space();
        for (int i = 0; i < _validationErrors.Count; i++)
            EditorGUILayout.HelpBox(_validationErrors[i], MessageType.Error);
    }

    private static void RebuildGradeGroups(SkillSetContainer container, HeroData heroData)
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild Grade Groups",
                "기존 등급 그룹과 항목을 삭제하고 도달 가능한 세 그룹을 다시 만듭니다.",
                "Rebuild",
                "Cancel"))
        {
            return;
        }

        Undo.RecordObject(container, "Rebuild Skill Grade Groups");
        container.GradeSets = new List<HeroGradeSkillSet>(3);

        for (int offset = 0; offset < 3; offset++)
        {
            container.GradeSets.Add(new HeroGradeSkillSet
            {
                Grade = (HeroGrade)((int)heroData.BaseGrade + offset),
                Sets = new List<HeroSkillSet>()
            });
        }

        EditorUtility.SetDirty(container);
    }

    private static void SortAllGroups(SkillSetContainer container)
    {
        Undo.RecordObject(container, "Sort Skill Sets By Level");

        for (int i = 0; i < container.GradeSets.Count; i++)
        {
            HeroGradeSkillSet group = container.GradeSets[i];
            if (group?.Sets == null)
                continue;

            group.Sets = group.Sets
                .Select((entry, index) => new { Entry = entry, Index = index })
                .OrderBy(item => item.Entry?.Level ?? int.MaxValue)
                .ThenBy(item => item.Index)
                .Select(item => item.Entry)
                .ToList();
        }

        EditorUtility.SetDirty(container);
    }

    private static HeroData FindHeroData(int heroUID)
    {
        string absolutePath = Path.GetFullPath(HeroDataPath);
        if (!File.Exists(absolutePath))
            return null;

        try
        {
            List<HeroData> heroes = JsonConvert.DeserializeObject<List<HeroData>>(
                File.ReadAllText(absolutePath));
            return heroes?.FirstOrDefault(hero => hero != null && hero.UID == heroUID);
        }
        catch
        {
            return null;
        }
    }
}
#endif
