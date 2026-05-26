#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Skill.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using SkillVfxSystem = Skill.Data.VisualEffectData;

public enum SkillNodeKind
{
    ActiveSkill,
    Trigger,
    Execution,
    Target,
    ExecutionVfx,
    Effect,
    ProjectileData
}

public sealed class SkillNodeView : Node
{
    private readonly ActiveSkillSO _skill;
    private readonly Color _accent;
    private readonly string _baseTitle;
    private readonly Dictionary<string, Port> _inputPorts = new Dictionary<string, Port>();
    private readonly Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();
    private readonly Dictionary<string, UnityEditor.UIElements.ObjectField> _slotFields = new Dictionary<string, UnityEditor.UIElements.ObjectField>();
    private Vector2 _lastExpandedSize;
    private bool _isApplyingCollapsedState;
    private const float CollapsedHeight = 32f;

    public string Key { get; }
    public SkillNodeKind Kind { get; }
    public UnityEngine.Object Asset { get; private set; }
    public int EffectIndex { get; }
    public bool IsLoose { get; }
    public Action<SkillNodeView, string, UnityEngine.Object> OnFieldSlotAssigned { get; set; }
    public Action<UnityEngine.Object, bool> OnAssetChanged { get; set; }
    public Action<SkillNodeView, string> OnAssetRenameRequested { get; set; }
    public Action OnRefreshRequested { get; set; }
    public Action<SkillNodeView> OnExpandedStateChanged { get; set; }

    public override bool expanded
    {
        get => base.expanded;
        set
        {
            if (base.expanded == value)
            {
                return;
            }

            if (base.expanded && !value)
            {
                RememberExpandedSize(GetPosition());
            }

            base.expanded = value;
            RefreshExpandedState();
            ApplyCollapsedState();

            if (value)
            {
                RestoreExpandedSize();
            }

            OnExpandedStateChanged?.Invoke(this);
        }
    }

    public SkillNodeView(
        ActiveSkillSO skill,
        SkillNodeKind kind,
        string key,
        string titleText,
        UnityEngine.Object asset,
        int effectIndex,
        int effectPortCount,
        Color accent,
        bool isLoose)
    {
        _skill = skill;
        Kind = kind;
        Key = key;
        Asset = asset;
        EffectIndex = effectIndex;
        IsLoose = isLoose;
        _accent = accent;
        _baseTitle = titleText;

        RefreshTitle();
        viewDataKey = "skill-node-" + key;

        capabilities |= Capabilities.Resizable;
        style.minWidth = 260f;
        style.minHeight = 120f;
        titleContainer.style.backgroundColor = new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 0.95f);
        titleContainer.style.borderTopColor = accent;
        titleContainer.style.borderTopWidth = 2f;

        CreatePorts();
        CreateInspector();
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        RefreshExpandedState();
        RefreshPorts();
        ApplyCollapsedState();
    }

    public Port GetInputPort(string portName)
    {
        return _inputPorts.TryGetValue(portName, out Port port) ? port : null;
    }

    public Port GetOutputPort(string portName)
    {
        return _outputPorts.TryGetValue(portName, out Port port) ? port : null;
    }

    public void SetFieldValueWithoutNotify(string slotName, UnityEngine.Object value)
    {
        if (_slotFields.TryGetValue(slotName, out UnityEditor.UIElements.ObjectField field))
        {
            field.SetValueWithoutNotify(value);
        }
    }

    public void RefreshTitle()
    {
        title = BuildTitle(_baseTitle);
    }

    public void InitializeLayoutRect(Rect rect)
    {
        RememberExpandedSize(rect);
    }

    public Rect GetLayoutRect()
    {
        Rect rect = GetPosition();
        if (!expanded && _lastExpandedSize.x > 0f && _lastExpandedSize.y > 0f)
        {
            rect.width = _lastExpandedSize.x;
            rect.height = _lastExpandedSize.y;
        }

        return rect;
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (_isApplyingCollapsedState || !expanded)
        {
            return;
        }

        RememberExpandedSize(GetPosition());
    }

    private void RememberExpandedSize(Rect rect)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        _lastExpandedSize = new Vector2(rect.width, rect.height);
    }

    private void RestoreExpandedSize()
    {
        if (_lastExpandedSize.x <= 0f || _lastExpandedSize.y <= 0f)
        {
            return;
        }

        Rect current = GetPosition();
        SetPosition(new Rect(current.x, current.y, _lastExpandedSize.x, _lastExpandedSize.y));
    }

    private void ApplyCollapsedState()
    {
        _isApplyingCollapsedState = true;

        DisplayStyle contentDisplay = expanded ? DisplayStyle.Flex : DisplayStyle.None;
        inputContainer.style.display = contentDisplay;
        outputContainer.style.display = contentDisplay;
        extensionContainer.style.display = contentDisplay;

        if (expanded)
        {
            capabilities |= Capabilities.Resizable;
            style.minHeight = 120f;
            SetResizerVisible(true);
        }
        else
        {
            capabilities &= ~Capabilities.Resizable;
            style.minHeight = CollapsedHeight;
            SetResizerVisible(false);

            Rect current = GetPosition();
            if (current.height > CollapsedHeight)
            {
                SetPosition(new Rect(current.x, current.y, current.width, CollapsedHeight));
            }
        }

        _isApplyingCollapsedState = false;
    }

    private void SetResizerVisible(bool visible)
    {
        Resizer resizer = this.Query<Resizer>();
        if (resizer != null)
        {
            resizer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private string BuildTitle(string label)
    {
        string assetName = Asset != null ? Asset.name : "None";
        if (Kind == SkillNodeKind.ActiveSkill)
        {
            assetName = _skill != null ? _skill.name : "None";
        }

        return label + " - " + assetName;
    }

    private void CreatePorts()
    {
        switch (Kind)
        {
            case SkillNodeKind.ActiveSkill:
                AddActiveSkillPropertyFields();
                AddFieldSlot("ActiveAction", "ActiveAction", typeof(ExecutionSystemData), _skill != null ? _skill.Execution : null);
                AddFieldSlot("Target", "Target", typeof(Skill.Data.TargetSystem), _skill != null ? _skill.Target : null);
                AddFieldSlot("Trigger", "Trigger", typeof(TriggerSystem), _skill != null ? _skill.Trigger : null);
                break;
            case SkillNodeKind.Trigger:
                AddOutputPort("Trigger", typeof(TriggerSystem));
                break;
            case SkillNodeKind.Execution:
                AddOutputPort("Execution", typeof(ExecutionSystemData));
                break;
            case SkillNodeKind.Target:
                AddOutputPort("Target", typeof(Skill.Data.TargetSystem));
                break;
            case SkillNodeKind.ExecutionVfx:
                AddOutputPort("VFX", typeof(SkillVfxSystem));
                break;
            case SkillNodeKind.Effect:
                AddOutputPort("Effect", typeof(EffectBase));
                break;
            case SkillNodeKind.ProjectileData:
                AddOutputPort("Projectile", typeof(ProjectileDataSO));
                break;
        }
    }

    private void AddActiveSkillPropertyFields()
    {
        var fields = new IMGUIContainer(DrawActiveSkillFields)
        {
            style =
            {
                minWidth = 250f,
                marginLeft = 4f,
                marginRight = 4f,
                marginBottom = 4f
            }
        };
        inputContainer.Add(fields);
    }

    private void AddFieldSlot(string label, string slotName, Type objectType, UnityEngine.Object value)
    {
        Port port = AddInputPort(slotName, objectType);
        port.portName = label;
        port.userData = new SkillNodePortData(slotName, objectType);

        var objectField = new UnityEditor.UIElements.ObjectField
        {
            objectType = objectType,
            allowSceneObjects = false,
            value = value
        };
        objectField.style.width = 165f;
        objectField.style.marginLeft = 4f;
        objectField.RegisterValueChangedCallback(evt => OnFieldSlotAssigned?.Invoke(this, slotName, evt.newValue));
        port.Add(objectField);
        _slotFields[slotName] = objectField;
    }

    private Port AddInputPort(string portName, Type portType, Port.Capacity capacity = Port.Capacity.Single)
    {
        Port port = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, portType);
        port.portName = portName;
        port.userData = new SkillNodePortData(portName, portType);
        inputContainer.Add(port);
        _inputPorts[portName] = port;
        return port;
    }

    private Port AddOutputPort(string portName, Type portType)
    {
        Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, portType);
        port.portName = portName;
        port.userData = new SkillNodePortData(string.Empty, portType);
        outputContainer.Add(port);
        _outputPorts[portName] = port;
        return port;
    }

    private void CreateInspector()
    {
        var accentBar = new VisualElement
        {
            style =
            {
                height = 2f,
                backgroundColor = _accent,
                marginBottom = 4f
            }
        };
        extensionContainer.Add(accentBar);

        CreateAssetNameField();

        if (Kind == SkillNodeKind.ActiveSkill)
        {
            return;
        }

        if (Kind == SkillNodeKind.Execution)
        {
            CreateExecutionBodyFields();
        }
        else if (Kind == SkillNodeKind.Effect)
        {
            CreateEffectBodyFields();
        }

        var inspector = new IMGUIContainer(DrawInspector)
        {
            style =
            {
                paddingLeft = 4f,
                paddingRight = 4f,
                paddingBottom = 6f
            }
        };
        extensionContainer.Add(inspector);
    }

    private void CreateExecutionBodyFields()
    {
        CreateEffectsFoldout();
        AddBodyFieldSlot("VFX", "VFX", typeof(SkillVfxSystem), Asset is ExecutionSystemData execution ? execution.VFX : null);
        if (Asset is ProjectileSkill projectileSkill)
        {
            AddBodyFieldSlot("ProjectileData", "ProjectileData", typeof(ProjectileDataSO), projectileSkill.ProjectileData);
        }
    }

    private void CreateEffectBodyFields()
    {
        AddBodyFieldSlot("VFX", "VFX", typeof(SkillVfxSystem), Asset is EffectBase effect ? effect.VFX : null);
    }

    private void AddBodyFieldSlot(string label, string slotName, Type objectType, UnityEngine.Object value)
    {
        var row = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginLeft = 4f,
                marginRight = 4f,
                marginBottom = 4f
            }
        };

        Port port = CreateBodyInputPort(slotName, objectType, Port.Capacity.Single, label);
        row.Add(port);

        var objectField = new UnityEditor.UIElements.ObjectField
        {
            objectType = objectType,
            allowSceneObjects = false,
            value = value
        };
        objectField.style.flexGrow = 1f;
        objectField.style.marginLeft = 4f;
        objectField.RegisterValueChangedCallback(evt => OnFieldSlotAssigned?.Invoke(this, slotName, evt.newValue));
        row.Add(objectField);

        _slotFields[slotName] = objectField;
        extensionContainer.Add(row);
    }

    private Port CreateBodyInputPort(string slotName, Type objectType, Port.Capacity capacity, string label)
    {
        Port port = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, objectType);
        port.portName = label;
        port.userData = new SkillNodePortData(slotName, objectType);
        port.style.flexShrink = 0f;
        _inputPorts[slotName] = port;
        return port;
    }

    private void CreateEffectsFoldout()
    {
        var foldout = new Foldout
        {
            text = "Effects",
            value = true
        };
        foldout.style.marginLeft = 4f;
        foldout.style.marginRight = 4f;
        foldout.style.marginBottom = 4f;

        ExecutionSystemData execution = Asset as ExecutionSystemData;
        int count = execution?.Effects != null ? execution.Effects.Count : 0;
        for (int i = 0; i < count; i++)
        {
            foldout.Add(CreateEffectEntryRow(i));
        }

        var appendRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                justifyContent = Justify.FlexEnd,
                marginTop = 2f
            }
        };

        var addButton = new Button(AddEmptyEffectEntry)
        {
            text = "Add Effect"
        };
        addButton.style.width = 84f;
        addButton.style.height = 20f;
        appendRow.Add(addButton);

        foldout.Add(appendRow);
        extensionContainer.Add(foldout);
    }

    private VisualElement CreateEffectEntryRow(int index)
    {
        var container = new VisualElement
        {
            style =
            {
                marginTop = 2f,
                marginBottom = 4f,
                minHeight = 46f
            }
        };

        var effectRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center
            }
        };

        effectRow.Add(CreateBodyInputPort(GetEffectSlotName(index), typeof(EffectBase), Port.Capacity.Single, "Effect " + (index + 1)));

        var effectField = new IMGUIContainer(() => DrawExecutionEffectProperty(index, "Effect", true))
        {
            style =
            {
                flexGrow = 1f,
                marginLeft = 4f
            }
        };
        effectRow.Add(effectField);

        var removeButton = new Button(() => RemoveEffectEntry(index))
        {
            text = "-"
        };
        removeButton.style.width = 24f;
        removeButton.style.height = 20f;
        removeButton.style.marginLeft = 4f;
        effectRow.Add(removeButton);

        var chanceRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginTop = 2f
            }
        };

        var chanceSpacer = new VisualElement
        {
            style =
            {
                width = 76f,
                flexShrink = 0f
            }
        };
        chanceRow.Add(chanceSpacer);

        var chanceField = new IMGUIContainer(() => DrawExecutionEffectProperty(index, "Chance", false))
        {
            style =
            {
                flexGrow = 1f,
                minHeight = 20f,
                marginLeft = 4f
            }
        };
        chanceRow.Add(chanceField);

        container.Add(effectRow);
        container.Add(chanceRow);
        return container;
    }

    private void CreateAssetNameField()
    {
        UnityEngine.Object renameTarget = GetRenameTarget();
        if (renameTarget == null)
        {
            return;
        }

        var nameField = new TextField("Asset Name")
        {
            value = renameTarget.name,
            isDelayed = true
        };
        nameField.style.marginLeft = 4f;
        nameField.style.marginRight = 4f;
        nameField.style.marginBottom = 4f;
        if (Kind == SkillNodeKind.ActiveSkill)
        {
            nameField.labelElement.style.minWidth = 72f;
            nameField.labelElement.style.width = 72f;
            nameField.labelElement.style.marginRight = 2f;
        }

        nameField.RegisterValueChangedCallback(evt =>
        {
            UnityEngine.Object currentTarget = GetRenameTarget();
            if (currentTarget == null || evt.newValue == currentTarget.name)
            {
                return;
            }

            OnAssetRenameRequested?.Invoke(this, evt.newValue);
            currentTarget = GetRenameTarget();
            nameField.SetValueWithoutNotify(currentTarget != null ? currentTarget.name : string.Empty);
            RefreshTitle();
        });
        extensionContainer.Add(nameField);
    }

    private UnityEngine.Object GetRenameTarget()
    {
        return Kind == SkillNodeKind.ActiveSkill ? _skill : Asset;
    }

    private void DrawInspector()
    {
        switch (Kind)
        {
            case SkillNodeKind.Trigger:
            case SkillNodeKind.Target:
            case SkillNodeKind.ExecutionVfx:
            case SkillNodeKind.ProjectileData:
                DrawSerializedObject(Asset);
                break;
            case SkillNodeKind.Execution:
                DrawSerializedObject(Asset, "Effects", "VFX", "ProjectileData");
                break;
            case SkillNodeKind.Effect:
                if (EffectIndex >= 0)
                {
                    DrawEffectEntry();
                }
                else
                {
                    DrawSerializedObject(Asset, "VFX");
                }
                break;
        }
    }

    private void DrawActiveSkillFields()
    {
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 82f;
        DrawSerializedObject(_skill, "ActiveAction", "Target", "Trigger");
        EditorGUIUtility.labelWidth = previousLabelWidth;
    }

    private void DrawEffectEntry()
    {
        if (_skill?.Execution == null || _skill.Execution.Effects == null || EffectIndex < 0 || EffectIndex >= _skill.Execution.Effects.Count)
        {
            EditorGUILayout.HelpBox("Effect entry is missing.", MessageType.Warning);
            return;
        }

        EffectBase effect = _skill.Execution.Effects[EffectIndex].Effect;
        if (effect != null)
        {
            DrawSerializedObject(effect, "VFX");
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an effect asset.", MessageType.Info);
        }
    }

    private void DrawExecutionEffectProperty(int index, string relativePropertyName, bool structureChanged)
    {
        if (!(Asset is ExecutionSystemData execution) || execution.Effects == null || index < 0 || index >= execution.Effects.Count)
        {
            EditorGUILayout.HelpBox("Effect entry is missing.", MessageType.Warning);
            return;
        }

        var serializedObject = new SerializedObject(execution);
        serializedObject.Update();

        SerializedProperty effectsProperty = serializedObject.FindProperty("Effects");
        SerializedProperty entryProperty = effectsProperty.GetArrayElementAtIndex(index);
        SerializedProperty property = entryProperty.FindPropertyRelative(relativePropertyName);
        if (property == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUI.BeginChangeCheck();
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = relativePropertyName == "Chance" ? 54f : 44f;
        EditorGUILayout.PropertyField(property, true);
        EditorGUIUtility.labelWidth = previousLabelWidth;
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(execution, "Edit Effect Entry");
            serializedObject.ApplyModifiedProperties();
            SkillGraphAssetUtility.MarkDirty(execution);
            OnAssetChanged?.Invoke(execution, structureChanged);
            return;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddEmptyEffectEntry()
    {
        if (!(Asset is ExecutionSystemData execution))
        {
            return;
        }

        Undo.RecordObject(execution, "Add Effect Entry");
        if (execution.Effects == null)
        {
            execution.Effects = new List<EffectEntry>();
        }

        execution.Effects.Add(new EffectEntry());
        SkillGraphAssetUtility.MarkDirty(execution);
        OnRefreshRequested?.Invoke();
    }

    private void RemoveEffectEntry(int index)
    {
        if (!(Asset is ExecutionSystemData execution) || execution.Effects == null || index < 0 || index >= execution.Effects.Count)
        {
            return;
        }

        Undo.RecordObject(execution, "Remove Effect Entry");
        execution.Effects.RemoveAt(index);
        SkillGraphAssetUtility.MarkDirty(execution);
        OnRefreshRequested?.Invoke();
    }

    private void DrawSerializedObject(UnityEngine.Object target, params string[] skippedPropertyPaths)
    {
        if (target == null)
        {
            return;
        }

        var serializedObject = new SerializedObject(target);
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        bool changed = false;
        bool structureChanged = false;
        while (property.NextVisible(enterChildren))
        {
            string propertyPath = property.propertyPath;
            if (ShouldSkipProperty(propertyPath, skippedPropertyPaths))
            {
                enterChildren = false;
                continue;
            }

            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(property, true);
                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                    structureChanged |= IsStructureProperty(propertyPath);
                }
            }

            enterChildren = false;
        }

        if (changed)
        {
            Undo.RecordObject(target, "Edit Skill Node");
            serializedObject.ApplyModifiedProperties();
            SkillGraphAssetUtility.MarkDirty(target);
            OnAssetChanged?.Invoke(target, structureChanged);
            return;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSerializedProperty(UnityEngine.Object target, string propertyPath, bool structureChanged)
    {
        if (target == null)
        {
            return;
        }

        var serializedObject = new SerializedObject(target);
        serializedObject.Update();

        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(property, true);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Edit Skill Node");
            serializedObject.ApplyModifiedProperties();
            SkillGraphAssetUtility.MarkDirty(target);
            OnAssetChanged?.Invoke(target, structureChanged);
            return;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static bool ShouldSkipProperty(string propertyPath, string[] skippedPropertyPaths)
    {
        if (skippedPropertyPaths == null)
        {
            return false;
        }

        for (int i = 0; i < skippedPropertyPaths.Length; i++)
        {
            if (propertyPath == skippedPropertyPaths[i])
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStructureProperty(string propertyPath)
    {
        return propertyPath == "ActiveAction"
            || propertyPath == "Target"
            || propertyPath == "Trigger"
            || propertyPath == "VFX"
            || propertyPath == "ProjectileData"
            || propertyPath == "Effect"
            || propertyPath.StartsWith("Effects");
    }

    public static string GetEffectSlotName(int index)
    {
        return "Effects[" + index + "]";
    }
}

public sealed class SkillNodePortData
{
    public readonly string SlotName;
    public readonly Type ValueType;

    public SkillNodePortData(string slotName, Type valueType)
    {
        SlotName = slotName;
        ValueType = valueType;
    }
}
#endif
