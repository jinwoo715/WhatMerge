#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using WhatMerge.Enemies;

namespace WhatMerge.Stage.Editor
{
    public sealed class StageDataEditorWindow : EditorWindow
    {
        private enum StageEditorTab
        {
            Stage,
            Waves,
            Mimic
        }

        private const string StageAssetDirectory = "Assets/07.SciptableObjects/Stage";
        private const float LeftPanelWidth = 230f;
        private const float RightPanelWidth = 300f;
        private const int WaveColumns = 10;

        private static readonly Color PanelColor = new Color32(42, 42, 42, 255);
        private static readonly Color BorderColor = new Color32(65, 65, 65, 255);
        private static readonly Color SelectedColor = new Color32(53, 79, 112, 255);
        private static readonly Color NormalWaveColor = new Color32(67, 79, 94, 255);
        private static readonly Color BossWaveColor = new Color32(166, 65, 48, 255);
        private static readonly Color MissingWaveColor = new Color32(57, 57, 57, 255);

        private readonly StageEditorCatalog _catalog = new StageEditorCatalog();
        private readonly StageEditorSpriteResolver _spriteResolver = new StageEditorSpriteResolver();
        private readonly List<StageData> _stageAssets = new List<StageData>();
        private readonly List<StageData> _filteredStageAssets = new List<StageData>();
        private readonly List<Button> _tabButtons = new List<Button>();

        private StageData _selectedStage;
        private SerializedObject _serializedStage;
        private StageEditorTab _activeTab = StageEditorTab.Waves;
        private int _selectedWaveNumber = 1;
        private int _fillStartWave = 1;
        private int _fillEndWave = 1;
        private WaveData _copiedWave;
        private string _stageSearch = string.Empty;
        private bool _hasUnsavedChanges;

        private ObjectField _stageObjectField;
        private VisualElement _stageListContainer;
        private ScrollView _mainContent;
        private ScrollView _validationPanel;
        private Label _sourceLabel;
        private Label _saveStateLabel;

        [MenuItem("Tools/Stage Data Editor")]
        public static void Open()
        {
            StageDataEditorWindow window = GetWindow<StageDataEditorWindow>("Stage Data Editor");
            window.minSize = new Vector2(1100f, 680f);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
            EditorApplication.projectChanged += HandleProjectChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            EditorApplication.projectChanged -= HandleProjectChanged;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is StageData stageData && stageData != _selectedStage)
                SelectStage(stageData);
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.backgroundColor = PanelColor;

            ReloadData();
            BuildLayout();
            SelectInitialStage();
        }

        private void BuildLayout()
        {
            rootVisualElement.Add(CreateTopToolbar());

            VisualElement body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.style.minHeight = 0f;

            body.Add(CreateStageSidebar());
            body.Add(CreateCenterPanel());
            body.Add(CreateValidationSidebar());

            rootVisualElement.Add(body);
            rootVisualElement.Add(CreateStatusBar());
        }

        private VisualElement CreateTopToolbar()
        {
            Toolbar toolbar = new Toolbar();
            toolbar.style.height = 38f;
            toolbar.style.flexShrink = 0f;

            Label assetLabel = new Label("Stage Asset");
            assetLabel.style.marginLeft = 8f;
            assetLabel.style.marginRight = 6f;
            assetLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(assetLabel);

            _stageObjectField = new ObjectField
            {
                objectType = typeof(StageData),
                allowSceneObjects = false
            };
            _stageObjectField.style.width = 360f;
            _stageObjectField.RegisterValueChangedCallback(evt => SelectStage(evt.newValue as StageData));
            toolbar.Add(_stageObjectField);

            ToolbarButton refreshButton = new ToolbarButton(RefreshWindow)
            {
                text = "Refresh",
                tooltip = "Reload StageData assets and EnemyData CSV."
            };
            toolbar.Add(refreshButton);

            ToolbarButton validateButton = new ToolbarButton(ValidateSelectedStage)
            {
                text = "Validate",
                tooltip = "Run stage data validation."
            };
            toolbar.Add(validateButton);

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);

            ToolbarButton saveButton = new ToolbarButton(SaveSelectedStage)
            {
                text = "Save Asset",
                tooltip = "Save the selected StageData asset."
            };
            saveButton.style.marginRight = 8f;
            toolbar.Add(saveButton);

            return toolbar;
        }

        private VisualElement CreateStageSidebar()
        {
            VisualElement sidebar = new VisualElement();
            sidebar.style.width = LeftPanelWidth;
            sidebar.style.flexShrink = 0f;
            sidebar.style.borderRightWidth = 1f;
            sidebar.style.borderRightColor = BorderColor;
            sidebar.style.backgroundColor = (Color)new Color32(36, 36, 36, 255);

            Label title = CreatePanelTitle("Stages");
            sidebar.Add(title);

            ToolbarSearchField searchField = new ToolbarSearchField();
            searchField.style.marginLeft = 8f;
            searchField.style.marginRight = 8f;
            searchField.style.marginBottom = 6f;
            searchField.RegisterValueChangedCallback(evt =>
            {
                _stageSearch = evt.newValue ?? string.Empty;
                RebuildStageList();
            });
            sidebar.Add(searchField);

            ScrollView stageListScroll = new ScrollView();
            stageListScroll.style.flexGrow = 1f;
            stageListScroll.style.minHeight = 0f;

            _stageListContainer = new VisualElement();
            stageListScroll.Add(_stageListContainer);
            sidebar.Add(stageListScroll);

            Button addButton = new Button(CreateStageAsset)
            {
                text = "+",
                tooltip = "Create a new StageData asset."
            };
            addButton.style.height = 30f;
            addButton.style.marginLeft = 8f;
            addButton.style.marginRight = 8f;
            addButton.style.marginTop = 6f;
            addButton.style.marginBottom = 8f;
            sidebar.Add(addButton);

            RebuildStageList();
            return sidebar;
        }

        private VisualElement CreateCenterPanel()
        {
            VisualElement center = new VisualElement();
            center.style.flexGrow = 1f;
            center.style.minWidth = 520f;
            center.style.minHeight = 0f;

            center.Add(CreateTabBar());

            _mainContent = new ScrollView();
            _mainContent.style.flexGrow = 1f;
            _mainContent.style.minHeight = 0f;
            center.Add(_mainContent);

            return center;
        }

        private VisualElement CreateTabBar()
        {
            VisualElement tabBar = new VisualElement();
            tabBar.style.height = 36f;
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.flexShrink = 0f;
            tabBar.style.borderBottomWidth = 1f;
            tabBar.style.borderBottomColor = BorderColor;

            _tabButtons.Clear();
            AddTabButton(tabBar, StageEditorTab.Stage, "Stage");
            AddTabButton(tabBar, StageEditorTab.Waves, "Waves");
            AddTabButton(tabBar, StageEditorTab.Mimic, "Mimic");
            UpdateTabStyles();

            return tabBar;
        }

        private void AddTabButton(VisualElement tabBar, StageEditorTab tab, string label)
        {
            Button button = new Button(() => SelectTab(tab)) { text = label };
            button.style.width = 130f;
            button.style.height = 35f;
            button.style.borderTopLeftRadius = 0f;
            button.style.borderTopRightRadius = 0f;
            button.style.borderBottomLeftRadius = 0f;
            button.style.borderBottomRightRadius = 0f;
            _tabButtons.Add(button);
            tabBar.Add(button);
        }

        private VisualElement CreateValidationSidebar()
        {
            _validationPanel = new ScrollView();
            _validationPanel.style.width = RightPanelWidth;
            _validationPanel.style.flexShrink = 0f;
            _validationPanel.style.borderLeftWidth = 1f;
            _validationPanel.style.borderLeftColor = BorderColor;
            _validationPanel.style.backgroundColor = (Color)new Color32(36, 36, 36, 255);
            RebuildValidationPanel();
            return _validationPanel;
        }

        private VisualElement CreateStatusBar()
        {
            VisualElement statusBar = new VisualElement();
            statusBar.style.height = 28f;
            statusBar.style.flexShrink = 0f;
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.borderTopWidth = 1f;
            statusBar.style.borderTopColor = BorderColor;
            statusBar.style.backgroundColor = (Color)new Color32(31, 31, 31, 255);

            _sourceLabel = new Label();
            _sourceLabel.style.marginLeft = 10f;
            _sourceLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            statusBar.Add(_sourceLabel);

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            statusBar.Add(spacer);

            _saveStateLabel = new Label();
            _saveStateLabel.style.marginRight = 10f;
            _saveStateLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            statusBar.Add(_saveStateLabel);

            RebuildStatusBar();
            return statusBar;
        }

        private void SelectInitialStage()
        {
            StageData initialStage = Selection.activeObject as StageData;
            if (initialStage == null && _stageAssets.Count > 0)
                initialStage = _stageAssets[0];

            SelectStage(initialStage);
        }

        private void SelectStage(StageData stageData)
        {
            _selectedStage = stageData;
            _serializedStage = stageData == null ? null : new SerializedObject(stageData);
            _hasUnsavedChanges = false;

            if (_selectedStage != null)
            {
                _selectedWaveNumber = Mathf.Clamp(_selectedWaveNumber, 1, Mathf.Max(1, _selectedStage.WaveCount));
                _fillStartWave = _selectedWaveNumber;
                _fillEndWave = _selectedWaveNumber;
                Selection.activeObject = _selectedStage;
            }

            _stageObjectField?.SetValueWithoutNotify(_selectedStage);
            RebuildStageList();
            RebuildMainContent();
            RebuildValidationPanel();
            RebuildStatusBar();
        }

        private void SelectTab(StageEditorTab tab)
        {
            _activeTab = tab;
            UpdateTabStyles();
            RebuildMainContent();
        }

        private void UpdateTabStyles()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool selected = i == (int)_activeTab;
                _tabButtons[i].style.backgroundColor = selected
                    ? (Color)new Color32(54, 65, 80, 255)
                    : (Color)new Color32(43, 43, 43, 255);
                _tabButtons[i].style.borderBottomWidth = selected ? 2f : 0f;
                _tabButtons[i].style.borderBottomColor = (Color)new Color32(62, 125, 202, 255);
            }
        }

        private void RebuildMainContent()
        {
            if (_mainContent == null)
                return;

            _mainContent.Clear();
            if (_selectedStage == null)
            {
                _mainContent.Add(CreateNotice("Select or create a StageData asset.", StageValidationSeverity.Info));
                return;
            }

            _serializedStage?.UpdateIfRequiredOrScript();

            switch (_activeTab)
            {
                case StageEditorTab.Stage:
                    BuildStageTab();
                    break;
                case StageEditorTab.Waves:
                    BuildWavesTab();
                    break;
                case StageEditorTab.Mimic:
                    BuildMimicTab();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void BuildStageTab()
        {
            VisualElement settings = CreateSection("Stage Settings");

            IntegerField uidField = new IntegerField("UID");
            uidField.SetValueWithoutNotify(_selectedStage.UID);
            uidField.SetEnabled(false);
            settings.Add(uidField);

            TextField nameField = new TextField("Name");
            nameField.SetValueWithoutNotify(_selectedStage.Name);
            nameField.RegisterValueChangedCallback(evt =>
                ApplyChange("Change Stage Name", () => _selectedStage.Name = evt.newValue, false, true));
            settings.Add(nameField);

            TextField descriptionField = new TextField("Description") { multiline = true };
            descriptionField.SetValueWithoutNotify(_selectedStage.Description);
            descriptionField.style.minHeight = 72f;
            descriptionField.RegisterValueChangedCallback(evt =>
                ApplyChange("Change Stage Description", () => _selectedStage.Description = evt.newValue));
            settings.Add(descriptionField);

            VisualElement values = CreateTwoColumnContainer();
            values.Add(CreateFloatField(
                "Normal Duration",
                _selectedStage.NormalWaveDuration,
                value => _selectedStage.NormalWaveDuration = value));
            values.Add(CreateFloatField(
                "Boss Duration",
                _selectedStage.BossWaveDuration,
                value => _selectedStage.BossWaveDuration = value));
            values.Add(CreateIntegerField(
                "Max Enemies",
                _selectedStage.MaxEnemyCount,
                value => _selectedStage.MaxEnemyCount = value));
            values.Add(CreateIntegerField(
                "Wave Count",
                _selectedStage.WaveCount,
                value =>
                {
                    _selectedStage.WaveCount = value;
                    _selectedWaveNumber = Mathf.Clamp(_selectedWaveNumber, 1, Mathf.Max(1, value));
                },
                true));
            settings.Add(values);
            _mainContent.Add(settings);

            VisualElement summary = CreateSection("Stage Summary");
            VisualElement metrics = new VisualElement();
            metrics.style.flexDirection = FlexDirection.Row;
            metrics.Add(CreateMetric("Normal Waves", CountWaves(WaveType.Normal).ToString()));
            metrics.Add(CreateMetric("Boss Waves", CountWaves(WaveType.Boss).ToString()));
            metrics.Add(CreateMetric(
                "Mimic",
                _selectedStage.MimicChallenge != null && _selectedStage.MimicChallenge.IsEnabled
                    ? "Configured"
                    : "Not configured"));
            summary.Add(metrics);

            Label assetPath = new Label($"Asset: {AssetDatabase.GetAssetPath(_selectedStage)}");
            assetPath.style.marginTop = 10f;
            assetPath.style.color = (Color)new Color32(170, 170, 170, 255);
            summary.Add(assetPath);
            _mainContent.Add(summary);
        }

        private void BuildWavesTab()
        {
            VisualElement timeline = CreateSection("Wave Timeline");

            VisualElement legend = new VisualElement();
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.marginBottom = 8f;
            legend.Add(CreateLegend("Normal", NormalWaveColor));
            legend.Add(CreateLegend("Boss", BossWaveColor));
            legend.Add(CreateLegend("Missing", MissingWaveColor));
            timeline.Add(legend);

            int waveCount = Mathf.Max(0, _selectedStage.WaveCount);
            for (int rowStart = 1; rowStart <= waveCount; rowStart += WaveColumns)
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;

                for (int column = 0; column < WaveColumns; column++)
                {
                    int waveNumber = rowStart + column;
                    if (waveNumber <= waveCount)
                    {
                        int capturedWave = waveNumber;
                        WaveData wave = FindWave(waveNumber);
                        Button waveButton = new Button(() =>
                        {
                            _selectedWaveNumber = capturedWave;
                            _fillStartWave = capturedWave;
                            _fillEndWave = capturedWave;
                            RebuildMainContent();
                        })
                        {
                            text = wave == null || wave.WaveType == WaveType.Normal
                                ? waveNumber.ToString()
                                : $"{waveNumber}  B",
                            tooltip = wave == null
                                ? $"Wave {waveNumber}: Missing"
                                : $"Wave {waveNumber}: {wave.WaveType}"
                        };

                        StyleWaveButton(waveButton, wave, waveNumber == _selectedWaveNumber);
                        row.Add(waveButton);
                    }
                    else
                    {
                        VisualElement placeholder = new VisualElement();
                        placeholder.style.flexGrow = 1f;
                        placeholder.style.flexBasis = 0f;
                        placeholder.style.marginLeft = 2f;
                        placeholder.style.marginRight = 2f;
                        row.Add(placeholder);
                    }
                }

                timeline.Add(row);
            }

            _mainContent.Add(timeline);

            WaveData selectedWave = FindWave(_selectedWaveNumber);
            if (selectedWave == null)
            {
                VisualElement missing = CreateSection($"Wave {_selectedWaveNumber}");
                missing.Add(CreateNotice(
                    $"Wave {_selectedWaveNumber} has no data.",
                    StageValidationSeverity.Warning));

                VisualElement actionRow = new VisualElement();
                actionRow.style.flexDirection = FlexDirection.Row;

                Button createButton = new Button(() => CreateWave(_selectedWaveNumber))
                {
                    text = $"Create Wave {_selectedWaveNumber}"
                };
                createButton.style.marginTop = 8f;
                actionRow.Add(createButton);

                Button pasteButton = new Button(() => CreateWave(_selectedWaveNumber, _copiedWave))
                {
                    text = "Paste Wave Settings"
                };
                pasteButton.SetEnabled(_copiedWave != null);
                pasteButton.style.marginTop = 8f;
                pasteButton.style.marginLeft = 4f;
                actionRow.Add(pasteButton);

                missing.Add(actionRow);
                _mainContent.Add(missing);
                return;
            }

            BuildSelectedWaveEditor(selectedWave);
        }

        private void BuildSelectedWaveEditor(WaveData wave)
        {
            VisualElement section = CreateSection($"Wave {_selectedWaveNumber} - {wave.WaveType}");

            VisualElement actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.justifyContent = Justify.FlexEnd;

            Button copyButton = new Button(() => CopyWave(wave))
            {
                text = "Copy"
            };
            copyButton.style.marginLeft = 4f;
            actionRow.Add(copyButton);

            Button pasteButton = new Button(() => PasteWave(wave))
            {
                text = "Paste"
            };
            pasteButton.SetEnabled(_copiedWave != null);
            pasteButton.style.marginLeft = 4f;
            actionRow.Add(pasteButton);

            Button deleteButton = new Button(() => DeleteWave(wave))
            {
                text = "Delete Wave"
            };
            deleteButton.style.marginLeft = 4f;
            actionRow.Add(deleteButton);
            section.Add(actionRow);

            VisualElement fields = CreateTwoColumnContainer();

            IntegerField waveIndexField = new IntegerField("Wave Index");
            waveIndexField.SetValueWithoutNotify(wave.WaveIndex + 1);
            waveIndexField.SetEnabled(false);
            fields.Add(waveIndexField);

            EnumField waveTypeField = new EnumField("Wave Type", wave.WaveType);
            waveTypeField.RegisterValueChangedCallback(evt =>
                ApplyChange("Change Wave Type", () => wave.WaveType = (WaveType)evt.newValue, true));
            fields.Add(waveTypeField);

            section.Add(fields);

            VisualElement fillRow = new VisualElement();
            fillRow.style.flexDirection = FlexDirection.Row;
            fillRow.style.alignItems = Align.FlexEnd;
            fillRow.style.marginTop = 10f;

            IntegerField fillStartField = new IntegerField("Fill From");
            fillStartField.SetValueWithoutNotify(_fillStartWave);
            fillStartField.style.width = 140f;
            fillStartField.RegisterValueChangedCallback(evt => _fillStartWave = evt.newValue);
            fillRow.Add(fillStartField);

            IntegerField fillEndField = new IntegerField("Fill To");
            fillEndField.SetValueWithoutNotify(_fillEndWave);
            fillEndField.style.width = 140f;
            fillEndField.style.marginLeft = 6f;
            fillEndField.RegisterValueChangedCallback(evt => _fillEndWave = evt.newValue);
            fillRow.Add(fillEndField);

            Button fillButton = new Button(() => FillWaveRange(wave))
            {
                text = "Fill Range"
            };
            fillButton.style.marginLeft = 6f;
            fillRow.Add(fillButton);
            section.Add(fillRow);

            Label spawnTitle = new Label("Spawn Entries");
            spawnTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            spawnTitle.style.fontSize = 13f;
            spawnTitle.style.marginTop = 12f;
            spawnTitle.style.marginBottom = 4f;
            section.Add(spawnTitle);

            IMGUIContainer spawnListContainer = CreateSpawnList(wave);
            section.Add(spawnListContainer);
            _mainContent.Add(section);
        }

        private void BuildMimicTab()
        {
            EnsureMimicData();
            MimicChallengeData challenge = _selectedStage.MimicChallenge;

            VisualElement settings = CreateSection("Challenge Settings");
            Label state = new Label(challenge.IsEnabled ? "Status: Configured" : "Status: Not configured");
            state.style.color = challenge.IsEnabled
                ? (Color)new Color32(91, 190, 106, 255)
                : (Color)new Color32(180, 180, 180, 255);
            state.style.marginBottom = 8f;
            settings.Add(state);

            VisualElement fields = CreateThreeColumnContainer();
            fields.Add(CreateFloatField(
                "Cooldown",
                challenge.Cooldown,
                value => challenge.Cooldown = value));
            fields.Add(CreateFloatField(
                "Time Limit",
                challenge.TimeLimit,
                value => challenge.TimeLimit = value));
            fields.Add(CreateIntegerField(
                "Bonus Currency",
                challenge.BonusBattleCurrency,
                value => challenge.BonusBattleCurrency = value));
            settings.Add(fields);

            Label rewardNote = new Label(
                "Base Kill Gold comes from EnemyData. Bonus Currency is granted in addition to it.");
            rewardNote.style.whiteSpace = WhiteSpace.Normal;
            rewardNote.style.color = (Color)new Color32(170, 170, 170, 255);
            rewardNote.style.marginTop = 8f;
            settings.Add(rewardNote);
            _mainContent.Add(settings);

            VisualElement entries = CreateSection("Summon Order");
            IReadOnlyList<EnemyData> mimicEnemies = _catalog.GetEnemies(EnemyType.Mimic);
            if (mimicEnemies.Count == 0)
            {
                entries.Add(CreateNotice(
                    "EnemyData has no Mimic entry. Add one before configuring this list.",
                    StageValidationSeverity.Info));
            }

            entries.Add(CreateMimicList());
            _mainContent.Add(entries);
        }

        private IMGUIContainer CreateSpawnList(WaveData wave)
        {
            int waveListIndex = _selectedStage.Waves.IndexOf(wave);
            SerializedProperty wavesProperty = _serializedStage.FindProperty(nameof(StageData.Waves));
            SerializedProperty spawnProperty = wavesProperty
                .GetArrayElementAtIndex(waveListIndex)
                .FindPropertyRelative(nameof(WaveData.SpawnDatas));

            ReorderableList list = new ReorderableList(_serializedStage, spawnProperty, true, true, true, true);
            list.drawHeaderCallback = rect => DrawSpawnHeader(rect);
            list.drawElementCallback = (rect, index, active, focused) =>
                DrawSpawnElement(rect, spawnProperty, index, wave.WaveType);
            list.onCanAddCallback = _ => GetExpectedEnemies(wave.WaveType).Count > 0;
            list.onAddCallback = _ => AddSpawnEntry(spawnProperty, wave.WaveType);
            list.onRemoveCallback = target =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(target);
                ApplySerializedChanges();
            };
            list.onReorderCallback = _ => ApplySerializedChanges();

            IMGUIContainer container = new IMGUIContainer(() =>
            {
                if (_selectedStage == null || _serializedStage == null)
                    return;

                _serializedStage.Update();
                EditorGUI.BeginChangeCheck();
                list.DoLayoutList();
                bool changed = EditorGUI.EndChangeCheck();
                changed |= _serializedStage.ApplyModifiedProperties();
                if (changed)
                    MarkSerializedChanges();
            });
            container.style.marginTop = 2f;
            return container;
        }

        private IMGUIContainer CreateMimicList()
        {
            SerializedProperty challengeProperty = _serializedStage.FindProperty(nameof(StageData.MimicChallenge));
            SerializedProperty entriesProperty = challengeProperty.FindPropertyRelative(nameof(MimicChallengeData.Entries));

            ReorderableList list = new ReorderableList(_serializedStage, entriesProperty, true, true, true, true)
            {
                elementHeight = 56f
            };
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Order     Enemy / Preview / Base Reward");
            list.drawElementCallback = (rect, index, active, focused) =>
                DrawMimicElement(rect, entriesProperty, index);
            list.onCanAddCallback = _ => _catalog.GetEnemies(EnemyType.Mimic).Count > 0;
            list.onAddCallback = _ => AddMimicEntry(entriesProperty);
            list.onRemoveCallback = target =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(target);
                ApplySerializedChanges(true);
            };
            list.onReorderCallback = _ => ApplySerializedChanges(true);

            IMGUIContainer container = new IMGUIContainer(() =>
            {
                if (_selectedStage == null || _serializedStage == null)
                    return;

                _serializedStage.Update();
                EditorGUI.BeginChangeCheck();
                list.DoLayoutList();
                bool changed = EditorGUI.EndChangeCheck();
                changed |= _serializedStage.ApplyModifiedProperties();
                if (changed)
                    MarkSerializedChanges();
            });
            return container;
        }

        private void DrawSpawnHeader(Rect rect)
        {
            float enemyWidth = rect.width * 0.43f;
            float numericWidth = (rect.width - enemyWidth) / 3f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, enemyWidth, rect.height), "Enemy");
            EditorGUI.LabelField(new Rect(rect.x + enemyWidth, rect.y, numericWidth, rect.height), "Start Delay");
            EditorGUI.LabelField(new Rect(rect.x + enemyWidth + numericWidth, rect.y, numericWidth, rect.height), "Count");
            EditorGUI.LabelField(new Rect(rect.x + enemyWidth + numericWidth * 2f, rect.y, numericWidth, rect.height), "Interval");
        }

        private void DrawSpawnElement(
            Rect rect,
            SerializedProperty spawnEntries,
            int index,
            WaveType waveType)
        {
            SerializedProperty entry = spawnEntries.GetArrayElementAtIndex(index);
            SerializedProperty enemyUID = entry.FindPropertyRelative(nameof(EnemySpawnData.EnemyUID));
            SerializedProperty startDelay = entry.FindPropertyRelative(nameof(EnemySpawnData.StartDelay));
            SerializedProperty spawnCount = entry.FindPropertyRelative(nameof(EnemySpawnData.SpawnCount));
            SerializedProperty spawnInterval = entry.FindPropertyRelative(nameof(EnemySpawnData.SpawnInterval));

            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            float gap = 4f;
            float enemyWidth = rect.width * 0.43f;
            float numericWidth = (rect.width - enemyWidth - gap * 3f) / 3f;

            Rect enemyRect = new Rect(rect.x, rect.y, enemyWidth, rect.height);
            Rect delayRect = new Rect(enemyRect.xMax + gap, rect.y, numericWidth, rect.height);
            Rect countRect = new Rect(delayRect.xMax + gap, rect.y, numericWidth, rect.height);
            Rect intervalRect = new Rect(countRect.xMax + gap, rect.y, numericWidth, rect.height);

            enemyUID.intValue = DrawEnemyPopup(
                enemyRect,
                enemyUID.intValue,
                GetExpectedEnemies(waveType));
            EditorGUI.PropertyField(delayRect, startDelay, GUIContent.none);
            EditorGUI.PropertyField(countRect, spawnCount, GUIContent.none);
            EditorGUI.PropertyField(intervalRect, spawnInterval, GUIContent.none);
        }

        private void DrawMimicElement(Rect rect, SerializedProperty entries, int index)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            SerializedProperty enemyUID = entry.FindPropertyRelative(nameof(MimicEntryData.EnemyUID));

            Rect orderRect = new Rect(rect.x, rect.y + 17f, 24f, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(orderRect, (index + 1).ToString());

            Rect previewRect = new Rect(orderRect.xMax + 4f, rect.y + 3f, 48f, 48f);
            DrawEnemyPreview(previewRect, enemyUID.intValue);

            float contentX = previewRect.xMax + 8f;
            Rect popupRect = new Rect(contentX, rect.y + 4f, rect.xMax - contentX, EditorGUIUtility.singleLineHeight);
            enemyUID.intValue = DrawEnemyPopup(
                popupRect,
                enemyUID.intValue,
                _catalog.GetEnemies(EnemyType.Mimic));

            Rect rewardRect = new Rect(
                contentX,
                popupRect.yMax + 5f,
                rect.xMax - contentX,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rewardRect, _catalog.GetRewardSummary(enemyUID.intValue), EditorStyles.miniLabel);
        }

        private int DrawEnemyPopup(
            Rect rect,
            int currentUID,
            IReadOnlyList<EnemyData> candidates)
        {
            if (candidates.Count == 0)
                return EditorGUI.IntField(rect, currentUID);

            bool currentIsCandidate = false;
            int selectedIndex = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].UID == currentUID)
                {
                    currentIsCandidate = true;
                    selectedIndex = i;
                    break;
                }
            }

            int offset = currentIsCandidate ? 0 : 1;
            string[] labels = new string[candidates.Count + offset];
            if (!currentIsCandidate)
            {
                labels[0] = _catalog.GetEnemyLabel(currentUID);
                selectedIndex = 0;
            }

            for (int i = 0; i < candidates.Count; i++)
                labels[i + offset] = _catalog.GetEnemyLabel(candidates[i].UID);

            int newIndex = EditorGUI.Popup(rect, selectedIndex, labels);
            if (!currentIsCandidate && newIndex == 0)
                return currentUID;

            return candidates[newIndex - offset].UID;
        }

        private void DrawEnemyPreview(Rect rect, int enemyUID)
        {
            EditorGUI.DrawRect(rect, new Color32(48, 48, 48, 255));
            if (!_catalog.TryGetEnemy(enemyUID, out EnemyData enemy))
                return;

            Sprite sprite = _spriteResolver.GetPreviewSprite(_selectedStage.UID, enemy.SpriteKey);
            if (sprite == null || sprite.texture == null)
                return;

            Rect textureRect = sprite.textureRect;
            Texture texture = sprite.texture;
            Rect uv = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
        }

        private void AddSpawnEntry(SerializedProperty spawnEntries, WaveType waveType)
        {
            IReadOnlyList<EnemyData> candidates = GetExpectedEnemies(waveType);
            if (candidates.Count == 0)
                return;

            _serializedStage.Update();
            int index = spawnEntries.arraySize;
            spawnEntries.InsertArrayElementAtIndex(index);
            SerializedProperty entry = spawnEntries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative(nameof(EnemySpawnData.EnemyUID)).intValue = candidates[0].UID;
            entry.FindPropertyRelative(nameof(EnemySpawnData.StartDelay)).floatValue = 0f;
            entry.FindPropertyRelative(nameof(EnemySpawnData.SpawnCount)).intValue = 1;
            entry.FindPropertyRelative(nameof(EnemySpawnData.SpawnInterval)).floatValue = 0.5f;
            ApplySerializedChanges();
        }

        private void AddMimicEntry(SerializedProperty entries)
        {
            IReadOnlyList<EnemyData> candidates = _catalog.GetEnemies(EnemyType.Mimic);
            if (candidates.Count == 0)
                return;

            _serializedStage.Update();
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative(nameof(MimicEntryData.EnemyUID)).intValue = candidates[0].UID;
            ApplySerializedChanges(true);
        }

        private void ApplySerializedChanges(bool rebuildMain = false)
        {
            _serializedStage.ApplyModifiedProperties();
            MarkSerializedChanges();
            if (rebuildMain)
                ScheduleMainContentRebuild();
        }

        private void MarkSerializedChanges()
        {
            if (_selectedStage == null)
                return;

            EditorUtility.SetDirty(_selectedStage);
            _hasUnsavedChanges = true;
            RebuildValidationPanel();
            RebuildStatusBar();
        }

        private void CreateWave(int waveNumber, WaveData template = null)
        {
            ApplyChange("Create Wave", () =>
            {
                _selectedStage.Waves ??= new List<WaveData>();
                WaveData wave = template == null
                    ? CreateDefaultWave(waveNumber)
                    : CloneWave(template, waveNumber - 1);

                _selectedStage.Waves.Add(wave);
                SortWaves();
                _selectedWaveNumber = waveNumber;
                _fillStartWave = waveNumber;
                _fillEndWave = waveNumber;
            }, true);
        }

        private WaveData CreateDefaultWave(int waveNumber)
        {
            WaveData wave = new WaveData
            {
                WaveIndex = waveNumber - 1,
                WaveType = WaveType.Normal,
                SpawnDatas = new List<EnemySpawnData>()
            };

            IReadOnlyList<EnemyData> enemies = _catalog.GetEnemies(EnemyType.Normal);
            if (enemies.Count > 0)
                wave.SpawnDatas.Add(new EnemySpawnData(enemies[0].UID, 1, 0f, 0.5f));

            return wave;
        }

        private void CopyWave(WaveData wave)
        {
            _copiedWave = CloneWave(wave, wave.WaveIndex);
            ShowNotification(new GUIContent($"Wave {wave.WaveIndex + 1} copied."));
            RebuildMainContent();
        }

        private void PasteWave(WaveData target)
        {
            ApplyWaveSettingsFrom(target, _copiedWave, "Paste Wave Settings");
        }

        private void ApplyWaveSettingsFrom(WaveData target, WaveData source, string undoName)
        {
            if (target == null || source == null)
                return;

            ApplyChange(undoName, () => CopyWaveSettings(source, target), true);
        }

        private void FillWaveRange(WaveData source)
        {
            int waveCount = Mathf.Max(1, _selectedStage.WaveCount);
            int startWave = Mathf.Clamp(Mathf.Min(_fillStartWave, _fillEndWave), 1, waveCount);
            int endWave = Mathf.Clamp(Mathf.Max(_fillStartWave, _fillEndWave), 1, waveCount);

            if (!EditorUtility.DisplayDialog(
                    "Fill Wave Range",
                    $"Apply wave {source.WaveIndex + 1} settings to waves {startWave}-{endWave}? Existing settings will be overwritten.",
                    "Fill",
                    "Cancel"))
            {
                return;
            }

            WaveData sourceSnapshot = CloneWave(source, source.WaveIndex);
            ApplyChange("Fill Wave Range", () =>
            {
                _selectedStage.Waves ??= new List<WaveData>();
                for (int waveIndex = startWave; waveIndex <= endWave; waveIndex++)
                {
                    WaveData target = FindWave(waveIndex);
                    if (target == null)
                        _selectedStage.Waves.Add(CloneWave(sourceSnapshot, waveIndex - 1));
                    else
                        CopyWaveSettings(sourceSnapshot, target);
                }

                SortWaves();
                _fillStartWave = startWave;
                _fillEndWave = endWave;
            }, true);
        }

        private static WaveData CloneWave(WaveData source, int waveIndex)
        {
            WaveData clone = new WaveData
            {
                WaveIndex = waveIndex
            };
            CopyWaveSettings(source, clone);
            return clone;
        }

        private static void CopyWaveSettings(WaveData source, WaveData target)
        {
            target.WaveType = source.WaveType;
            target.SpawnDatas = new List<EnemySpawnData>();

            if (source.SpawnDatas == null)
                return;

            for (int i = 0; i < source.SpawnDatas.Count; i++)
            {
                EnemySpawnData spawn = source.SpawnDatas[i];
                if (spawn == null)
                {
                    target.SpawnDatas.Add(null);
                    continue;
                }

                target.SpawnDatas.Add(new EnemySpawnData(
                    spawn.EnemyUID,
                    spawn.SpawnCount,
                    spawn.StartDelay,
                    spawn.SpawnInterval));
            }
        }

        private void SortWaves()
        {
            _selectedStage.Waves.Sort((left, right) =>
                (left?.WaveIndex ?? int.MaxValue).CompareTo(right?.WaveIndex ?? int.MaxValue));
        }

        private void DeleteWave(WaveData wave)
        {
            if (!EditorUtility.DisplayDialog(
                    "Delete Wave",
                    $"Delete wave {wave.WaveIndex + 1}?",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            ApplyChange("Delete Wave", () => _selectedStage.Waves.Remove(wave), true);
        }

        private void EnsureMimicData()
        {
            if (_selectedStage.MimicChallenge != null)
                return;

            Undo.RecordObject(_selectedStage, "Initialize Mimic Data");
            _selectedStage.MimicChallenge = new MimicChallengeData();
            EditorUtility.SetDirty(_selectedStage);
            _hasUnsavedChanges = true;
            _serializedStage = new SerializedObject(_selectedStage);
        }

        private void ApplyChange(
            string undoName,
            Action change,
            bool rebuildMain = false,
            bool rebuildStageList = false)
        {
            if (_selectedStage == null)
                return;

            Undo.RecordObject(_selectedStage, undoName);
            change();
            EditorUtility.SetDirty(_selectedStage);
            _hasUnsavedChanges = true;

            if (rebuildStageList)
                RebuildStageList();

            RebuildValidationPanel();
            RebuildStatusBar();

            if (rebuildMain)
                ScheduleMainContentRebuild();
        }

        private void ScheduleMainContentRebuild()
        {
            rootVisualElement.schedule.Execute(RebuildMainContent);
        }

        private void RebuildValidationPanel()
        {
            if (_validationPanel == null)
                return;

            _validationPanel.Clear();
            _validationPanel.Add(CreatePanelTitle("Validation"));

            List<StageValidationMessage> messages = StageDataEditorValidator.Validate(_selectedStage, _catalog);
            if (HasDuplicateStageUID())
            {
                messages.Insert(
                    0,
                    new StageValidationMessage(
                        StageValidationSeverity.Error,
                        $"Stage UID {_selectedStage.UID} is duplicated."));
            }

            DrawValidationSeverity(messages, StageValidationSeverity.Error);
            DrawValidationSeverity(messages, StageValidationSeverity.Warning);
            DrawValidationSeverity(messages, StageValidationSeverity.Success);
            DrawValidationSeverity(messages, StageValidationSeverity.Info);
        }

        private void DrawValidationSeverity(
            IReadOnlyList<StageValidationMessage> messages,
            StageValidationSeverity severity)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Severity == severity)
                    _validationPanel.Add(CreateNotice(messages[i].Text, severity));
            }
        }

        private void RebuildStageList()
        {
            if (_stageListContainer == null)
                return;

            FilterStageAssets();
            _stageListContainer.Clear();

            for (int i = 0; i < _filteredStageAssets.Count; i++)
            {
                StageData stage = _filteredStageAssets[i];
                Button item = new Button(() => SelectStage(stage))
                {
                    text = $"{stage.UID}   {stage.Name}",
                    tooltip = AssetDatabase.GetAssetPath(stage)
                };
                item.style.height = 28f;
                item.style.unityTextAlign = TextAnchor.MiddleLeft;
                item.style.borderTopLeftRadius = 0f;
                item.style.borderTopRightRadius = 0f;
                item.style.borderBottomLeftRadius = 0f;
                item.style.borderBottomRightRadius = 0f;
                item.style.marginLeft = 4f;
                item.style.marginRight = 4f;
                item.style.backgroundColor = stage == _selectedStage
                    ? SelectedColor
                    : (Color)new Color32(42, 42, 42, 255);
                _stageListContainer.Add(item);
            }
        }

        private void RebuildStatusBar()
        {
            if (_sourceLabel == null || _saveStateLabel == null)
                return;

            _sourceLabel.text = _selectedStage == null
                ? "Source: None"
                : $"Source: {AssetDatabase.GetAssetPath(_selectedStage)}";

            bool dirty = _selectedStage != null && (_hasUnsavedChanges || EditorUtility.IsDirty(_selectedStage));
            _saveStateLabel.text = dirty ? "Unsaved changes" : "All changes saved";
            _saveStateLabel.style.color = dirty
                ? (Color)new Color32(235, 178, 82, 255)
                : (Color)new Color32(91, 190, 106, 255);
        }

        private void RefreshWindow()
        {
            string selectedPath = _selectedStage == null ? null : AssetDatabase.GetAssetPath(_selectedStage);
            ReloadData();

            StageData selected = string.IsNullOrEmpty(selectedPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<StageData>(selectedPath);
            if (selected == null && _stageAssets.Count > 0)
                selected = _stageAssets[0];

            SelectStage(selected);
        }

        private void ValidateSelectedStage()
        {
            List<StageValidationMessage> messages = StageDataEditorValidator.Validate(_selectedStage, _catalog);
            RebuildValidationPanel();
            bool hasErrors = StageDataEditorValidator.HasErrors(messages) || HasDuplicateStageUID();
            ShowNotification(new GUIContent(hasErrors
                ? "Validation found errors."
                : "StageData is valid."));
        }

        private void SaveSelectedStage()
        {
            if (_selectedStage == null)
                return;

            _serializedStage?.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selectedStage);
            AssetDatabase.SaveAssets();
            _hasUnsavedChanges = false;
            RebuildStatusBar();
            ShowNotification(new GUIContent("StageData saved."));
        }

        private void CreateStageAsset()
        {
            int nextUID = GetNextStageUID();
            string path = EditorUtility.SaveFilePanelInProject(
                "Create StageData",
                $"Stage_{nextUID}",
                "asset",
                "Choose a location for the StageData asset.",
                StageAssetDirectory);
            if (string.IsNullOrEmpty(path))
                return;

            StageData stage = CreateInstance<StageData>();
            stage.UID = nextUID;
            stage.Name = "New Stage";
            stage.Description = string.Empty;
            stage.NormalWaveDuration = 20f;
            stage.BossWaveDuration = 60f;
            stage.MaxEnemyCount = 100;
            stage.WaveCount = 60;
            stage.Waves = new List<WaveData>();
            stage.MimicChallenge = new MimicChallengeData();

            AssetDatabase.CreateAsset(stage, path);
            AssetDatabase.SaveAssets();
            ReloadData();
            SelectStage(stage);
        }

        private void ReloadData()
        {
            _catalog.Reload();
            _spriteResolver.Clear();
            LoadStageAssets();
        }

        private void LoadStageAssets()
        {
            _stageAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { StageAssetDirectory });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage != null)
                    _stageAssets.Add(stage);
            }

            _stageAssets.Sort((left, right) =>
            {
                int uidComparison = left.UID.CompareTo(right.UID);
                return uidComparison != 0
                    ? uidComparison
                    : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            });
            FilterStageAssets();
        }

        private void FilterStageAssets()
        {
            _filteredStageAssets.Clear();
            for (int i = 0; i < _stageAssets.Count; i++)
            {
                StageData stage = _stageAssets[i];
                if (string.IsNullOrWhiteSpace(_stageSearch)
                    || stage.UID.ToString().Contains(_stageSearch)
                    || (!string.IsNullOrEmpty(stage.Name)
                        && stage.Name.IndexOf(_stageSearch, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _filteredStageAssets.Add(stage);
                }
            }
        }

        private int GetNextStageUID()
        {
            int maxUID = 0;
            for (int i = 0; i < _stageAssets.Count; i++)
                maxUID = Mathf.Max(maxUID, _stageAssets[i].UID);

            return maxUID + 1;
        }

        private bool HasDuplicateStageUID()
        {
            if (_selectedStage == null)
                return false;

            int matches = 0;
            for (int i = 0; i < _stageAssets.Count; i++)
            {
                if (_stageAssets[i].UID == _selectedStage.UID)
                    matches++;
            }

            return matches > 1;
        }

        private int CountWaves(WaveType waveType)
        {
            int count = 0;
            if (_selectedStage?.Waves == null)
                return count;

            for (int i = 0; i < _selectedStage.Waves.Count; i++)
            {
                WaveData wave = _selectedStage.Waves[i];
                if (wave != null && wave.WaveType == waveType)
                    count++;
            }

            return count;
        }

        private WaveData FindWave(int waveNumber)
        {
            if (_selectedStage?.Waves == null)
                return null;

            for (int i = 0; i < _selectedStage.Waves.Count; i++)
            {
                WaveData wave = _selectedStage.Waves[i];
                if (wave != null && wave.WaveIndex == waveNumber - 1)
                    return wave;
            }

            return null;
        }

        private IReadOnlyList<EnemyData> GetExpectedEnemies(WaveType waveType)
        {
            return _catalog.GetWaveEnemies(waveType);
        }

        private void HandleUndoRedo()
        {
            if (_selectedStage != null)
                _serializedStage = new SerializedObject(_selectedStage);

            RebuildMainContent();
            RebuildValidationPanel();
            RebuildStatusBar();
        }

        private void HandleProjectChanged()
        {
            if (rootVisualElement == null || rootVisualElement.childCount == 0)
                return;

            rootVisualElement.schedule.Execute(RefreshWindow);
        }

        private static Label CreatePanelTitle(string text)
        {
            Label title = new Label(text);
            title.style.height = 38f;
            title.style.marginLeft = 10f;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 13f;
            return title;
        }

        private static VisualElement CreateSection(string titleText)
        {
            VisualElement section = new VisualElement();
            section.style.marginLeft = 12f;
            section.style.marginRight = 12f;
            section.style.marginTop = 10f;
            section.style.marginBottom = 2f;
            section.style.paddingLeft = 12f;
            section.style.paddingRight = 12f;
            section.style.paddingTop = 10f;
            section.style.paddingBottom = 12f;
            section.style.borderLeftWidth = 1f;
            section.style.borderRightWidth = 1f;
            section.style.borderTopWidth = 1f;
            section.style.borderBottomWidth = 1f;
            section.style.borderLeftColor = BorderColor;
            section.style.borderRightColor = BorderColor;
            section.style.borderTopColor = BorderColor;
            section.style.borderBottomColor = BorderColor;
            section.style.backgroundColor = (Color)new Color32(39, 39, 39, 255);

            Label title = new Label(titleText);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14f;
            title.style.marginBottom = 8f;
            section.Add(title);
            return section;
        }

        private static VisualElement CreateTwoColumnContainer()
        {
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexWrap = Wrap.Wrap;
            return container;
        }

        private static VisualElement CreateThreeColumnContainer()
        {
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            return container;
        }

        private VisualElement CreateFloatField(string label, float value, Action<float> setter)
        {
            FloatField field = new FloatField(label);
            field.SetValueWithoutNotify(value);
            field.style.flexGrow = 1f;
            field.style.flexBasis = 0f;
            field.style.marginRight = 6f;
            field.RegisterValueChangedCallback(evt =>
                ApplyChange($"Change {label}", () => setter(evt.newValue)));
            return field;
        }

        private VisualElement CreateIntegerField(
            string label,
            int value,
            Action<int> setter,
            bool rebuildMain = false)
        {
            IntegerField field = new IntegerField(label);
            field.SetValueWithoutNotify(value);
            field.style.flexGrow = 1f;
            field.style.flexBasis = 0f;
            field.style.marginRight = 6f;
            field.RegisterValueChangedCallback(evt =>
                ApplyChange($"Change {label}", () => setter(evt.newValue), rebuildMain));
            return field;
        }

        private static VisualElement CreateMetric(string label, string value)
        {
            VisualElement metric = new VisualElement();
            metric.style.flexGrow = 1f;
            metric.style.flexBasis = 0f;
            metric.style.marginRight = 6f;
            metric.style.paddingLeft = 8f;
            metric.style.paddingRight = 8f;
            metric.style.paddingTop = 8f;
            metric.style.paddingBottom = 8f;
            metric.style.backgroundColor = (Color)new Color32(47, 47, 47, 255);

            Label labelElement = new Label(label);
            labelElement.style.color = (Color)new Color32(165, 165, 165, 255);
            metric.Add(labelElement);

            Label valueElement = new Label(value);
            valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueElement.style.fontSize = 13f;
            valueElement.style.marginTop = 3f;
            metric.Add(valueElement);
            return metric;
        }

        private static VisualElement CreateLegend(string label, Color color)
        {
            VisualElement item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.marginRight = 14f;

            VisualElement swatch = new VisualElement();
            swatch.style.width = 14f;
            swatch.style.height = 14f;
            swatch.style.marginRight = 5f;
            swatch.style.backgroundColor = color;
            item.Add(swatch);
            item.Add(new Label(label));
            return item;
        }

        private static VisualElement CreateNotice(string text, StageValidationSeverity severity)
        {
            Color accent;
            Color background;
            switch (severity)
            {
                case StageValidationSeverity.Success:
                    accent = new Color32(74, 174, 91, 255);
                    background = new Color32(37, 57, 41, 255);
                    break;
                case StageValidationSeverity.Warning:
                    accent = new Color32(221, 164, 65, 255);
                    background = new Color32(62, 52, 34, 255);
                    break;
                case StageValidationSeverity.Error:
                    accent = new Color32(215, 82, 82, 255);
                    background = new Color32(64, 37, 37, 255);
                    break;
                default:
                    accent = new Color32(128, 151, 181, 255);
                    background = new Color32(41, 48, 58, 255);
                    break;
            }

            VisualElement notice = new VisualElement();
            notice.style.marginLeft = 8f;
            notice.style.marginRight = 8f;
            notice.style.marginBottom = 6f;
            notice.style.paddingLeft = 8f;
            notice.style.paddingRight = 8f;
            notice.style.paddingTop = 7f;
            notice.style.paddingBottom = 7f;
            notice.style.borderLeftWidth = 3f;
            notice.style.borderLeftColor = accent;
            notice.style.backgroundColor = background;

            Label label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            notice.Add(label);
            return notice;
        }

        private static void StyleWaveButton(Button button, WaveData wave, bool selected)
        {
            button.style.flexGrow = 1f;
            button.style.flexBasis = 0f;
            button.style.height = 34f;
            button.style.marginLeft = 2f;
            button.style.marginRight = 2f;
            button.style.marginTop = 2f;
            button.style.marginBottom = 2f;
            button.style.borderTopLeftRadius = 2f;
            button.style.borderTopRightRadius = 2f;
            button.style.borderBottomLeftRadius = 2f;
            button.style.borderBottomRightRadius = 2f;
            button.style.backgroundColor = wave == null
                ? MissingWaveColor
                : wave.WaveType == WaveType.Boss
                    ? BossWaveColor
                    : NormalWaveColor;

            float borderWidth = selected ? 2f : 0f;
            Color borderColor = selected ? Color.white : Color.clear;
            button.style.borderLeftWidth = borderWidth;
            button.style.borderRightWidth = borderWidth;
            button.style.borderTopWidth = borderWidth;
            button.style.borderBottomWidth = borderWidth;
            button.style.borderLeftColor = borderColor;
            button.style.borderRightColor = borderColor;
            button.style.borderTopColor = borderColor;
            button.style.borderBottomColor = borderColor;
        }
    }
}
#endif
