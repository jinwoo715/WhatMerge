#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Skill.Data;
using UnityEditor;
using UnityEngine;
using WhatMerge.Heros;

[CustomEditor(typeof(SkillSetContainer))]
public class SkillSetContainerEditor : Editor
{
    private const string HeroDataPath = "Assets/06.Data/JSON/HeroData.json";
    private const string GameConfigPath = "Assets/03.Prefabs/Config/GameConfig.asset";
    private const float PurposeWidth = 145f;
    private const float LevelWidth = 48f;
    private const float RemoveButtonWidth = 22f;

    private enum SlotKind
    {
        BasicAttack,
        ActiveUnlock,
        PassiveProgression,
        ActiveEnhancement,
        TriggerReduction
    }

    private enum TriggerKind
    {
        None,
        Mana,
        HitCount
    }

    private enum SlotKey
    {
        BasicAttack,
        Active1Unlock,
        Active2Unlock,
        Active3Unlock,
        Passive1Unlock,
        Passive1Plus,
        Passive1PlusPlus,
        Passive2Unlock,
        Passive2Plus,
        Passive2PlusPlus,
        Active1Plus,
        Active1PlusPlus,
        Active2Plus,
        Active2PlusPlus,
        Active3Plus,
        Active3PlusPlus,
        ManaReduction,
        HitCountReduction
    }

    private sealed class SlotDefinition
    {
        public readonly int Level;
        public readonly string Label;
        public readonly SlotKind Kind;
        public readonly SlotKey Key;
        public readonly int TargetNumber;
        public readonly TriggerKind Trigger;

        public SlotDefinition(
            int level,
            string label,
            SlotKind kind,
            SlotKey key,
            int targetNumber = 0,
            TriggerKind trigger = TriggerKind.None)
        {
            Level = level;
            Label = label;
            Kind = kind;
            Key = key;
            TargetNumber = targetNumber;
            Trigger = trigger;
        }
    }

    private sealed class InspectorContext
    {
        private readonly Dictionary<SlotKey, UnityEngine.Object> _sharedReferences =
            new Dictionary<SlotKey, UnityEngine.Object>();
        private readonly HashSet<SlotKey> _sharedReferenceConflicts =
            new HashSet<SlotKey>();
        private readonly Dictionary<(HeroGrade Grade, int Number), ActiveSkillData> _activeSkills =
            new Dictionary<(HeroGrade Grade, int Number), ActiveSkillData>();
        private readonly Dictionary<(HeroGrade Grade, ActiveSkillData Skill), int> _activeUnlockLevels =
            new Dictionary<(HeroGrade Grade, ActiveSkillData Skill), int>();
        private readonly Dictionary<int, int> _levelOccurrences =
            new Dictionary<int, int>();

        public void Rebuild(SerializedProperty gradeSets)
        {
            _sharedReferences.Clear();
            _sharedReferenceConflicts.Clear();
            _activeSkills.Clear();
            _activeUnlockLevels.Clear();

            for (int groupIndex = 0; groupIndex < gradeSets.arraySize; groupIndex++)
            {
                SerializedProperty group = gradeSets.GetArrayElementAtIndex(groupIndex);
                HeroGrade grade = (HeroGrade)group.FindPropertyRelative("Grade").intValue;
                SerializedProperty sets = group.FindPropertyRelative("Sets");
                _levelOccurrences.Clear();

                for (int entryIndex = 0; entryIndex < sets.arraySize; entryIndex++)
                {
                    SerializedProperty entry = sets.GetArrayElementAtIndex(entryIndex);
                    int level = entry.FindPropertyRelative("Level").intValue;
                    int occurrence = TakeOccurrence(_levelOccurrences, level);
                    SlotDefinition slot = ResolveSlot(grade, level, occurrence);
                    UnityEngine.Object reference = entry.FindPropertyRelative("Skill")
                        .objectReferenceValue;

                    if (slot != null && reference != null)
                        RegisterSharedReference(slot.Key, reference);

                    if (reference is not ActiveSkillData activeSkill)
                        continue;

                    var unlockKey = (grade, activeSkill);
                    if (!_activeUnlockLevels.TryGetValue(unlockKey, out int unlockLevel)
                        || level < unlockLevel)
                    {
                        _activeUnlockLevels[unlockKey] = level;
                    }

                    if (slot?.Kind == SlotKind.ActiveUnlock)
                        _activeSkills[(grade, slot.TargetNumber)] = activeSkill;
                }
            }
        }

        public bool HasSharedReferenceConflict(SlotKey key)
        {
            return _sharedReferenceConflicts.Contains(key);
        }

        public ActiveSkillData GetActiveSkill(HeroGrade grade, int number)
        {
            _activeSkills.TryGetValue((grade, number), out ActiveSkillData skill);
            return skill;
        }

        public bool IsActiveUnlockedAtOrBefore(
            HeroGrade grade,
            ActiveSkillData skill,
            int level)
        {
            return _activeUnlockLevels.TryGetValue((grade, skill), out int unlockLevel)
                   && unlockLevel <= level;
        }

        private void RegisterSharedReference(SlotKey key, UnityEngine.Object reference)
        {
            if (!_sharedReferences.TryGetValue(key, out UnityEngine.Object registered))
            {
                _sharedReferences.Add(key, reference);
                return;
            }

            if (registered != reference)
                _sharedReferenceConflicts.Add(key);
        }
    }

    private static readonly IReadOnlyDictionary<HeroGrade, SlotDefinition[]> Templates =
        new Dictionary<HeroGrade, SlotDefinition[]>
        {
            [HeroGrade.D] = new[]
            {
                Basic(), Active(1, 1), Passive(10, 1, true), Enhance(20, 1, "+"),
                Passive(30, 1, false, "+"), Enhance(40, 1, "++"),
                Passive(60, 1, false, "++"), Reduce(100, TriggerKind.Mana),
                Reduce(150, TriggerKind.Mana)
            },
            [HeroGrade.C] = new[]
            {
                Basic(), Active(1, 1), Passive(10, 1, true), Active(20, 2),
                Enhance(30, 1, "+"), Passive(40, 1, false, "+"),
                Enhance(60, 2, "+"), Enhance(70, 1, "++"),
                Passive(80, 1, false, "++"), Enhance(90, 2, "++"),
                Reduce(100, TriggerKind.Mana), Reduce(150, TriggerKind.HitCount)
            },
            [HeroGrade.B] = new[]
            {
                Basic(), Active(1, 1), Passive(10, 1, true), Active(20, 2),
                Passive(30, 2, true), Enhance(40, 1, "+"),
                Passive(60, 1, false, "+"), Enhance(70, 2, "+"),
                Passive(80, 2, false, "+"), Enhance(90, 1, "++"),
                Reduce(100, TriggerKind.Mana), Passive(110, 1, false, "++"),
                Enhance(120, 2, "++"), Reduce(150, TriggerKind.HitCount)
            },
            [HeroGrade.A] = new[]
            {
                Basic(), Active(1, 1), Passive(10, 1, true), Active(20, 2),
                Passive(30, 2, true), Enhance(40, 1, "+"),
                Passive(60, 1, false, "+"), Enhance(70, 2, "+"),
                Passive(80, 2, false, "+"), Enhance(90, 1, "++"),
                Reduce(100, TriggerKind.Mana), Passive(110, 1, false, "++"),
                Enhance(120, 2, "++"), Passive(130, 2, false, "++"),
                Reduce(150, TriggerKind.HitCount)
            },
            [HeroGrade.S] = new[]
            {
                Basic(), Active(1, 1), Passive(10, 1, true), Active(20, 2),
                Passive(30, 2, true), Active(40, 3), Enhance(60, 1, "+"),
                Passive(70, 1, false, "+"), Enhance(80, 2, "+"),
                Passive(90, 2, false, "+"), Reduce(100, TriggerKind.Mana),
                Enhance(100, 3, "+"), Enhance(110, 1, "++"),
                Passive(120, 1, false, "++"), Enhance(130, 2, "++"),
                Passive(140, 2, false, "++"), Reduce(150, TriggerKind.HitCount),
                Enhance(150, 3, "++")
            }
        };

    private static readonly IReadOnlyDictionary<(HeroGrade Grade, int Level, int Occurrence), SlotDefinition>
        SlotLookup = BuildSlotLookup();

    private readonly Dictionary<HeroGrade, bool> _foldouts = new Dictionary<HeroGrade, bool>();
    private readonly List<string> _validationErrors = new List<string>();
    private readonly Dictionary<int, HeroData> _heroDataByUid = new Dictionary<int, HeroData>();
    private readonly Dictionary<int, int> _drawLevelOccurrences = new Dictionary<int, int>();
    private readonly InspectorContext _inspectorContext = new InspectorContext();
    private GameConfig _gameConfig;
    private string _externalDataError;
    private string _statusMessage;
    private MessageType _statusType;
    private bool _statusChangedThisDraw;

    private void OnEnable()
    {
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        ReloadExternalData();
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= OnProjectChanged;
    }

    private void OnProjectChanged()
    {
        ReloadExternalData();
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        _statusChangedThisDraw = false;
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("UID"));
        bool uidChanged = serializedObject.ApplyModifiedProperties();

        SkillSetContainer container = (SkillSetContainer)target;
        HeroData heroData = FindHeroData(container.UID);

        if (uidChanged)
            ClearValidationResult();

        if (!string.IsNullOrEmpty(_externalDataError))
            EditorGUILayout.HelpBox(_externalDataError, MessageType.Error);

        DrawHeroSummary(heroData);
        DrawToolbar(container, heroData, _gameConfig);

        serializedObject.Update();
        SerializedProperty gradeSets = serializedObject.FindProperty("GradeSets");

        if (heroData == null)
        {
            EditorGUILayout.HelpBox(
                "UID에 해당하는 HeroData를 먼저 지정해야 합니다.",
                MessageType.Info);
        }
        else if (!HasExpectedGradeStructure(container, heroData))
        {
            EditorGUILayout.HelpBox(
                "등급 그룹 구조가 올바르지 않습니다. Rebuild Grade Groups로 도달 가능한 세 등급을 다시 구성하세요.",
                MessageType.Error);
        }
        else
        {
            _inspectorContext.Rebuild(gradeSets);
            DrawGradeGroups(gradeSets, heroData, _inspectorContext);
        }

        bool dataChanged = serializedObject.ApplyModifiedProperties();
        if (dataChanged)
        {
            _validationErrors.Clear();
            if (!_statusChangedThisDraw)
                _statusMessage = null;
        }

        DrawStatus();
        DrawValidationErrors();
    }

    private void DrawToolbar(
        SkillSetContainer container,
        HeroData heroData,
        GameConfig gameConfig)
    {
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = heroData != null;
            if (GUILayout.Button("Rebuild Grade Groups"))
            {
                serializedObject.ApplyModifiedProperties();
                if (RebuildGradeGroups(container, heroData))
                {
                    SetStatus("등급 그룹을 다시 만들었습니다. 기존 스킬 항목은 삭제되었습니다.", MessageType.Warning);
                    ClearValidationErrors();
                }
            }

            GUI.enabled = heroData != null && HasExpectedGradeStructure(container, heroData);
            if (GUILayout.Button("Create Standard Slots"))
            {
                serializedObject.ApplyModifiedProperties();
                int populatedCount = PopulateEmptyStandardSlots(container, heroData);
                SetStatus(
                    populatedCount > 0
                        ? $"빈 등급 그룹 {populatedCount}개에 표준 슬롯을 생성했습니다."
                        : "표준 슬롯을 생성할 빈 등급 그룹이 없습니다.",
                    populatedCount > 0 ? MessageType.Info : MessageType.Warning);
                ClearValidationErrors();
            }

            GUI.enabled = true;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = container.GradeSets != null;
            if (GUILayout.Button("Sort All By Level"))
            {
                serializedObject.ApplyModifiedProperties();
                SortAllGroups(container);
                SetStatus("모든 등급의 슬롯을 레벨 순으로 정렬했습니다.", MessageType.Info);
                ClearValidationErrors();
            }

            GUI.enabled = heroData != null && gameConfig?.HeroProgression != null;
            if (GUILayout.Button("Validate"))
            {
                serializedObject.ApplyModifiedProperties();
                _validationErrors.Clear();
                _validationErrors.AddRange(
                    SkillSetValidator.Validate(
                        container,
                        heroData,
                        gameConfig.HeroProgression.MaxLevel));
                SetStatus(
                    _validationErrors.Count == 0
                        ? "SkillSet 검증을 통과했습니다."
                        : $"SkillSet 검증에서 오류 {_validationErrors.Count}개를 발견했습니다.",
                    _validationErrors.Count == 0 ? MessageType.Info : MessageType.Error);
            }

            GUI.enabled = true;
        }
    }

    private void DrawGradeGroups(
        SerializedProperty gradeSets,
        HeroData heroData,
        InspectorContext context)
    {
        EditorGUILayout.Space();

        for (int offset = 0; offset < 3; offset++)
        {
            HeroGrade grade = (HeroGrade)((int)heroData.BaseGrade + offset);
            SerializedProperty group = FindGradeGroup(gradeSets, grade);
            DrawGradeGroup(gradeSets, group, grade, context);
        }
    }

    private void DrawGradeGroup(
        SerializedProperty gradeSets,
        SerializedProperty group,
        HeroGrade grade,
        InspectorContext context)
    {
        if (!_foldouts.TryGetValue(grade, out bool expanded))
            expanded = true;

        SerializedProperty sets = group.FindPropertyRelative("Sets");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            expanded = EditorGUILayout.Foldout(
                expanded,
                $"{grade} Grade ({sets.arraySize})",
                true,
                EditorStyles.foldoutHeader);
            _foldouts[grade] = expanded;

            if (!expanded)
                return;

            DrawColumnHeaders();

            int removeIndex = -1;
            _drawLevelOccurrences.Clear();
            for (int i = 0; i < sets.arraySize; i++)
            {
                SerializedProperty entry = sets.GetArrayElementAtIndex(i);
                int level = entry.FindPropertyRelative("Level").intValue;
                int occurrence = TakeOccurrence(_drawLevelOccurrences, level);
                SlotDefinition slot = ResolveSlot(grade, level, occurrence);

                if (DrawSkillRow(gradeSets, sets, grade, i, slot, context))
                    removeIndex = i;
            }

            if (removeIndex >= 0)
                sets.DeleteArrayElementAtIndex(removeIndex);

            if (GUILayout.Button("Add Entry"))
                AddEntry(sets);
        }
    }

    private static void DrawColumnHeaders()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("용도", EditorStyles.miniBoldLabel, GUILayout.Width(PurposeWidth));
            GUILayout.Label("Level", EditorStyles.miniBoldLabel, GUILayout.Width(LevelWidth));
            GUILayout.Label("Skill", EditorStyles.miniBoldLabel);
            GUILayout.Space(RemoveButtonWidth);
        }
    }

    private bool DrawSkillRow(
        SerializedProperty gradeSets,
        SerializedProperty sets,
        HeroGrade grade,
        int index,
        SlotDefinition slot,
        InspectorContext context)
    {
        SerializedProperty entry = sets.GetArrayElementAtIndex(index);
        SerializedProperty level = entry.FindPropertyRelative("Level");
        SerializedProperty skill = entry.FindPropertyRelative("Skill");
        UnityEngine.Object previousReference = skill.objectReferenceValue;

        bool remove;
        using (new EditorGUILayout.HorizontalScope())
        {
            string purpose = slot?.Label ?? "추가 항목";
            EditorGUILayout.LabelField(
                new GUIContent(purpose, purpose),
                GUILayout.Width(PurposeWidth));
            EditorGUILayout.PropertyField(level, GUIContent.none, GUILayout.Width(LevelWidth));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(skill, GUIContent.none);
            if (EditorGUI.EndChangeCheck() && slot != null)
            {
                HandleSharedReferenceChange(gradeSets, slot, skill, previousReference);
                context.Rebuild(gradeSets);
            }
            remove = GUILayout.Button("-", GUILayout.Width(RemoveButtonWidth));
        }

        string error = GetRowError(context, grade, sets, index, slot);
        if (!string.IsNullOrEmpty(error))
            EditorGUILayout.HelpBox(error, MessageType.Error);

        return remove;
    }

    private void HandleSharedReferenceChange(
        SerializedProperty gradeSets,
        SlotDefinition slot,
        SerializedProperty source,
        UnityEngine.Object previousReference)
    {
        SkillBaseData newReference = source.objectReferenceValue as SkillBaseData;
        if (newReference == null)
        {
            int linkedReferenceCount = CountSharedReferences(gradeSets, slot.Key);
            if (linkedReferenceCount == 0)
                return;

            bool clearAll = EditorUtility.DisplayDialog(
                "Clear Shared Skill Reference",
                $"'{slot.Label}' 용도의 다른 슬롯 {linkedReferenceCount}개도 함께 해제합니다.",
                "Clear All",
                "Cancel");
            if (!clearAll)
            {
                source.objectReferenceValue = previousReference;
                return;
            }

            int clearedCount = SynchronizeSharedReference(gradeSets, slot.Key, null);
            SetStatus(
                $"'{slot.Label}' 용도의 참조 {clearedCount + 1}개를 해제했습니다.",
                MessageType.Info);
            return;
        }

        if (!CanSynchronize(slot, newReference))
            return;

        int synchronizedCount = SynchronizeSharedReference(
            gradeSets,
            slot.Key,
            newReference);
        SetStatus(
            $"'{slot.Label}' 용도의 다른 슬롯 {synchronizedCount}개에 참조를 연결했습니다.",
            MessageType.Info);
    }

    private int SynchronizeSharedReference(
        SerializedProperty gradeSets,
        SlotKey key,
        SkillBaseData reference)
    {
        Undo.RecordObject(target, "Synchronize Shared Skill References");
        int changedCount = 0;
        Dictionary<int, int> levelOccurrences = new Dictionary<int, int>();

        for (int groupIndex = 0; groupIndex < gradeSets.arraySize; groupIndex++)
        {
            SerializedProperty group = gradeSets.GetArrayElementAtIndex(groupIndex);
            HeroGrade grade = (HeroGrade)group.FindPropertyRelative("Grade").intValue;
            SerializedProperty sets = group.FindPropertyRelative("Sets");
            levelOccurrences.Clear();

            for (int entryIndex = 0; entryIndex < sets.arraySize; entryIndex++)
            {
                SerializedProperty entry = sets.GetArrayElementAtIndex(entryIndex);
                int level = entry.FindPropertyRelative("Level").intValue;
                int occurrence = TakeOccurrence(levelOccurrences, level);
                SlotDefinition candidate = ResolveSlot(grade, level, occurrence);
                if (candidate == null || candidate.Key != key)
                    continue;

                SerializedProperty skill = sets.GetArrayElementAtIndex(entryIndex)
                    .FindPropertyRelative("Skill");
                if (skill.objectReferenceValue == reference)
                    continue;

                skill.objectReferenceValue = reference;
                changedCount++;
            }
        }

        return changedCount;
    }

    private static int CountSharedReferences(
        SerializedProperty gradeSets,
        SlotKey key)
    {
        int count = 0;
        Dictionary<int, int> levelOccurrences = new Dictionary<int, int>();

        for (int groupIndex = 0; groupIndex < gradeSets.arraySize; groupIndex++)
        {
            SerializedProperty group = gradeSets.GetArrayElementAtIndex(groupIndex);
            HeroGrade grade = (HeroGrade)group.FindPropertyRelative("Grade").intValue;
            SerializedProperty sets = group.FindPropertyRelative("Sets");
            levelOccurrences.Clear();

            for (int entryIndex = 0; entryIndex < sets.arraySize; entryIndex++)
            {
                SerializedProperty entry = sets.GetArrayElementAtIndex(entryIndex);
                int level = entry.FindPropertyRelative("Level").intValue;
                int occurrence = TakeOccurrence(levelOccurrences, level);
                SlotDefinition candidate = ResolveSlot(grade, level, occurrence);
                if (candidate == null || candidate.Key != key)
                    continue;

                UnityEngine.Object reference = sets.GetArrayElementAtIndex(entryIndex)
                    .FindPropertyRelative("Skill").objectReferenceValue;
                if (reference != null)
                    count++;
            }
        }

        return count;
    }

    private static void AddEntry(SerializedProperty sets)
    {
        int index = sets.arraySize;
        int previousLevel = index == 0
            ? 0
            : sets.GetArrayElementAtIndex(index - 1).FindPropertyRelative("Level").intValue;

        sets.arraySize++;
        SerializedProperty entry = sets.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("Level").intValue = previousLevel;
        entry.FindPropertyRelative("Skill").objectReferenceValue = null;
    }

    private static string GetRowError(
        InspectorContext context,
        HeroGrade grade,
        SerializedProperty sets,
        int index,
        SlotDefinition slot)
    {
        SerializedProperty entry = sets.GetArrayElementAtIndex(index);
        int level = entry.FindPropertyRelative("Level").intValue;
        SkillBaseData skill = entry.FindPropertyRelative("Skill").objectReferenceValue as SkillBaseData;

        if (skill == null)
            return "Skill이 지정되지 않았습니다.";

        if (slot == null)
            return GetEnhancementOrderError(context, grade, level, skill);

        string typeError = GetSlotTypeError(slot, skill);
        if (!string.IsNullOrEmpty(typeError))
            return typeError;

        if (context.HasSharedReferenceConflict(slot.Key))
            return $"'{slot.Label}' 용도의 다른 슬롯에 서로 다른 에셋이 지정되어 있습니다.";

        switch (slot.Kind)
        {
            case SlotKind.BasicAttack:
                ActiveSkillData basicAttack = (ActiveSkillData)skill;
                if (basicAttack.Priority != 0
                    || basicAttack.Trigger is not NoneTriggerData
                    || !Mathf.Approximately(basicAttack.ActivationChance, 1f))
                {
                    return "기본 공격은 Priority 0, NoneTrigger, ActivationChance 1이어야 합니다.";
                }
                return null;

            case SlotKind.ActiveUnlock:
                ActiveSkillData activeSkill = (ActiveSkillData)skill;
                return activeSkill.Priority < 1
                    ? "해금 액티브 스킬의 Priority는 1 이상이어야 합니다."
                    : null;

            case SlotKind.PassiveProgression:
                if (skill is PassiveSkillData)
                    return null;

                if (GetEnhancementTarget(skill) == null)
                    return "강화 데이터에 대상 ActiveSkillData가 지정되지 않았습니다.";
                string passiveEnhancementError = GetEnhancementDataError(skill);
                if (!string.IsNullOrEmpty(passiveEnhancementError))
                    return passiveEnhancementError;
                return GetEnhancementOrderError(context, grade, level, skill);

            case SlotKind.ActiveEnhancement:
                ActiveSkillData target = GetEnhancementTarget(skill);
                if (target == null)
                    return "강화 데이터에 대상 ActiveSkillData가 지정되지 않았습니다.";

                ActiveSkillData expectedTarget = context.GetActiveSkill(grade, slot.TargetNumber);
                if (expectedTarget == null)
                    return $"스킬 {slot.TargetNumber} 해금 슬롯을 먼저 지정해야 합니다.";
                if (target != expectedTarget)
                    return $"이 슬롯은 스킬 {slot.TargetNumber}을 대상으로 해야 합니다.";
                string activeEnhancementError = GetEnhancementDataError(skill);
                if (!string.IsNullOrEmpty(activeEnhancementError))
                    return activeEnhancementError;
                return GetEnhancementOrderError(context, grade, level, skill);

            case SlotKind.TriggerReduction:
                TriggerRequirementReductionData reduction =
                    (TriggerRequirementReductionData)skill;
                if (reduction.TargetSkill == null)
                    return "요구량 감소 데이터에 대상 ActiveSkillData가 지정되지 않았습니다.";
                if (reduction.ReductionType != TriggerRequirementReductionType.Ratio
                    || !Mathf.Approximately(reduction.ReductionValue, 0.2f))
                {
                    return "요구량 감소 슬롯은 비율 0.2(20%)여야 합니다.";
                }
                if (slot.Trigger == TriggerKind.Mana
                    && reduction.TargetSkill.Trigger is not ManaTriggerData)
                {
                    return "마나 요구량 감소 슬롯의 대상은 ManaTrigger 스킬이어야 합니다.";
                }
                if (slot.Trigger == TriggerKind.HitCount
                    && reduction.TargetSkill.Trigger is not HitCountTriggerData)
                {
                    return "타수 요구량 감소 슬롯의 대상은 HitCountTrigger 스킬이어야 합니다.";
                }
                return GetEnhancementOrderError(context, grade, level, skill);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static string GetSlotTypeError(
        SlotDefinition slot,
        SkillBaseData skill)
    {
        return slot.Kind switch
        {
            SlotKind.BasicAttack when skill is not ActiveSkillData =>
                "기본 공격 슬롯에는 ActiveSkillData만 지정할 수 있습니다.",
            SlotKind.ActiveUnlock when skill is not ActiveSkillData =>
                "액티브 스킬 해금 슬롯에는 ActiveSkillData만 지정할 수 있습니다.",
            SlotKind.PassiveProgression
                when skill is not PassiveSkillData && !IsSupportedEnhancementData(skill) =>
                "패시브 성장 슬롯에는 PassiveSkillData 또는 지원되는 강화 데이터만 지정할 수 있습니다.",
            SlotKind.ActiveEnhancement when !IsSupportedEnhancementData(skill) =>
                "액티브 강화 슬롯에는 강화 데이터만 지정할 수 있습니다.",
            SlotKind.TriggerReduction when skill is not TriggerRequirementReductionData =>
                "요구량 감소 슬롯에는 TriggerRequirementReductionData만 지정할 수 있습니다.",
            _ => null
        };
    }

    private static bool CanSynchronize(
        SlotDefinition slot,
        SkillBaseData skill)
    {
        return string.IsNullOrEmpty(GetSlotTypeError(slot, skill));
    }

    private static bool IsSupportedEnhancementData(SkillBaseData skill)
    {
        return skill is Enhancer
               or ActivationChanceEnhanceData
               or SequenceCountEnhanceData
               or TriggerRequirementReductionData
               or ExtraEffectData;
    }

    private static string GetEnhancementDataError(SkillBaseData skill)
    {
        if (skill is not SequenceCountEnhanceData enhancer)
            return null;

        if (enhancer.AddCount < 1)
            return "연속 타격 증가 횟수는 1 이상이어야 합니다.";

        return enhancer.TargetSkill?.Execution is SequenceHitExecutionData
            ? null
            : "연속 타격 횟수 강화 대상은 SequenceHitExecutionData를 사용해야 합니다.";
    }

    private static string GetEnhancementOrderError(
        InspectorContext context,
        HeroGrade grade,
        int level,
        SkillBaseData skill)
    {
        ActiveSkillData target = GetEnhancementTarget(skill);
        if (target == null)
            return null;

        return context.IsActiveUnlockedAtOrBefore(grade, target, level)
            ? null
            : "강화 대상 액티브 스킬이 같은 레벨 또는 이전 레벨에 해금되어야 합니다.";
    }

    private static ActiveSkillData GetEnhancementTarget(SkillBaseData skill)
    {
        return skill switch
        {
            Enhancer enhancer => enhancer.TargetSkill,
            ActivationChanceEnhanceData enhancer => enhancer.TargetSkill,
            SequenceCountEnhanceData enhancer => enhancer.TargetSkill,
            TriggerRequirementReductionData enhancer => enhancer.TargetSkill,
            ExtraEffectData enhancer => enhancer.TargetSkill,
            _ => null
        };
    }

    private static SlotDefinition ResolveSlot(
        HeroGrade grade,
        int level,
        int occurrence)
    {
        SlotLookup.TryGetValue((grade, level, occurrence), out SlotDefinition slot);
        return slot;
    }

    private static int TakeOccurrence(
        Dictionary<int, int> occurrences,
        int level)
    {
        occurrences.TryGetValue(level, out int occurrence);
        occurrences[level] = occurrence + 1;
        return occurrence;
    }

    private static IReadOnlyDictionary<(HeroGrade Grade, int Level, int Occurrence), SlotDefinition>
        BuildSlotLookup()
    {
        var lookup =
            new Dictionary<(HeroGrade Grade, int Level, int Occurrence), SlotDefinition>();
        Dictionary<int, int> occurrences = new Dictionary<int, int>();

        foreach (KeyValuePair<HeroGrade, SlotDefinition[]> pair in Templates)
        {
            occurrences.Clear();
            for (int i = 0; i < pair.Value.Length; i++)
            {
                SlotDefinition slot = pair.Value[i];
                int occurrence = TakeOccurrence(occurrences, slot.Level);
                lookup.Add((pair.Key, slot.Level, occurrence), slot);
            }
        }

        return lookup;
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

    private void DrawStatus()
    {
        if (!string.IsNullOrEmpty(_statusMessage))
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
    }

    private void DrawValidationErrors()
    {
        for (int i = 0; i < _validationErrors.Count; i++)
            EditorGUILayout.HelpBox(_validationErrors[i], MessageType.Error);
    }

    private void SetStatus(string message, MessageType type)
    {
        _statusMessage = message;
        _statusType = type;
        _statusChangedThisDraw = true;
    }

    private void ClearValidationResult()
    {
        _statusMessage = null;
        _validationErrors.Clear();
    }

    private void ClearValidationErrors()
    {
        _validationErrors.Clear();
    }

    private static bool RebuildGradeGroups(SkillSetContainer container, HeroData heroData)
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild Grade Groups",
                "기존 등급 그룹과 항목을 삭제하고 도달 가능한 세 그룹을 다시 만듭니다.",
                "Rebuild",
                "Cancel"))
        {
            return false;
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
        return true;
    }

    private static int PopulateEmptyStandardSlots(
        SkillSetContainer container,
        HeroData heroData)
    {
        Undo.RecordObject(container, "Create Standard Skill Slots");
        int populatedCount = 0;

        for (int offset = 0; offset < 3; offset++)
        {
            HeroGrade grade = (HeroGrade)((int)heroData.BaseGrade + offset);
            HeroGradeSkillSet group = container.GradeSets.Single(item => item.Grade == grade);
            if (group.Sets != null && group.Sets.Count > 0)
                continue;

            group.Sets = Templates[grade]
                .Select(slot => new HeroSkillSet { Level = slot.Level })
                .ToList();
            populatedCount++;
        }

        if (populatedCount > 0)
            EditorUtility.SetDirty(container);

        return populatedCount;
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

    private static bool HasExpectedGradeStructure(
        SkillSetContainer container,
        HeroData heroData)
    {
        if (container.GradeSets == null || container.GradeSets.Count != 3)
            return false;

        for (int offset = 0; offset < 3; offset++)
        {
            HeroGrade expected = (HeroGrade)((int)heroData.BaseGrade + offset);
            int count = 0;

            for (int i = 0; i < container.GradeSets.Count; i++)
            {
                HeroGradeSkillSet group = container.GradeSets[i];
                if (group != null && group.Grade == expected)
                    count++;
            }

            if (count != 1)
                return false;
        }

        return true;
    }

    private static SerializedProperty FindGradeGroup(
        SerializedProperty gradeSets,
        HeroGrade grade)
    {
        for (int i = 0; i < gradeSets.arraySize; i++)
        {
            SerializedProperty group = gradeSets.GetArrayElementAtIndex(i);
            if (group.FindPropertyRelative("Grade").intValue == (int)grade)
                return group;
        }

        throw new InvalidOperationException($"Grade group {grade} was not found.");
    }

    private HeroData FindHeroData(int heroUID)
    {
        _heroDataByUid.TryGetValue(heroUID, out HeroData heroData);
        return heroData;
    }

    private void ReloadExternalData()
    {
        _heroDataByUid.Clear();
        _externalDataError = null;
        _gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

        TextAsset heroDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(HeroDataPath);
        if (heroDataAsset == null)
        {
            AddExternalDataError($"HeroData를 불러올 수 없습니다: {HeroDataPath}");
        }
        else
        {
            try
            {
                List<HeroData> heroes = JsonConvert.DeserializeObject<List<HeroData>>(
                    heroDataAsset.text);
                if (heroes == null)
                {
                    AddExternalDataError("HeroData JSON 결과가 null입니다.");
                }
                else
                {
                    for (int i = 0; i < heroes.Count; i++)
                    {
                        HeroData hero = heroes[i];
                        if (hero == null)
                            continue;

                        if (!_heroDataByUid.TryAdd(hero.UID, hero))
                            AddExternalDataError($"HeroData UID {hero.UID}가 중복되었습니다.");
                    }
                }
            }
            catch (Exception exception)
            {
                AddExternalDataError($"HeroData JSON 파싱에 실패했습니다: {exception.Message}");
            }
        }

        if (_gameConfig == null)
            AddExternalDataError($"GameConfig를 불러올 수 없습니다: {GameConfigPath}");
    }

    private void AddExternalDataError(string error)
    {
        _externalDataError = string.IsNullOrEmpty(_externalDataError)
            ? error
            : $"{_externalDataError}\n{error}";
    }

    private static SlotDefinition Basic()
    {
        return new SlotDefinition(
            0,
            "기본 공격",
            SlotKind.BasicAttack,
            SlotKey.BasicAttack);
    }

    private static SlotDefinition Active(int level, int number)
    {
        return new SlotDefinition(
            level,
            $"스킬 {number} 해금",
            SlotKind.ActiveUnlock,
            GetActiveUnlockKey(number),
            number);
    }

    private static SlotDefinition Passive(
        int level,
        int number,
        bool isUnlock,
        string suffix = null)
    {
        string label = isUnlock
            ? $"패시브 {number} 해금"
            : $"패시브 {number} {suffix}";
        return new SlotDefinition(
            level,
            label,
            SlotKind.PassiveProgression,
            GetPassiveKey(number, suffix),
            number);
    }

    private static SlotDefinition Enhance(int level, int number, string suffix)
    {
        return new SlotDefinition(
            level,
            $"스킬 {number} {suffix}",
            SlotKind.ActiveEnhancement,
            GetActiveEnhancementKey(number, suffix),
            number);
    }

    private static SlotDefinition Reduce(int level, TriggerKind trigger)
    {
        string label = trigger == TriggerKind.Mana
            ? "마나 요구량 -20%"
            : "타수 요구량 -20%";
        return new SlotDefinition(
            level,
            label,
            SlotKind.TriggerReduction,
            trigger == TriggerKind.Mana
                ? SlotKey.ManaReduction
                : SlotKey.HitCountReduction,
            trigger: trigger);
    }

    private static SlotKey GetActiveUnlockKey(int number)
    {
        return number switch
        {
            1 => SlotKey.Active1Unlock,
            2 => SlotKey.Active2Unlock,
            3 => SlotKey.Active3Unlock,
            _ => throw new ArgumentOutOfRangeException(nameof(number), number, null)
        };
    }

    private static SlotKey GetActiveEnhancementKey(int number, string suffix)
    {
        return (number, suffix) switch
        {
            (1, "+") => SlotKey.Active1Plus,
            (1, "++") => SlotKey.Active1PlusPlus,
            (2, "+") => SlotKey.Active2Plus,
            (2, "++") => SlotKey.Active2PlusPlus,
            (3, "+") => SlotKey.Active3Plus,
            (3, "++") => SlotKey.Active3PlusPlus,
            _ => throw new ArgumentOutOfRangeException(
                nameof(number),
                $"Unsupported active enhancement slot: {number}{suffix}")
        };
    }

    private static SlotKey GetPassiveKey(int number, string suffix)
    {
        return (number, suffix) switch
        {
            (1, null) => SlotKey.Passive1Unlock,
            (1, "+") => SlotKey.Passive1Plus,
            (1, "++") => SlotKey.Passive1PlusPlus,
            (2, null) => SlotKey.Passive2Unlock,
            (2, "+") => SlotKey.Passive2Plus,
            (2, "++") => SlotKey.Passive2PlusPlus,
            _ => throw new ArgumentOutOfRangeException(
                nameof(number),
                $"Unsupported passive slot: {number}{suffix}")
        };
    }
}
#endif
