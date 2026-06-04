#if UNITY_EDITOR
using Skill.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SkillVfxSystem = Skill.Data.VFXData;

public class SkillEditorWindow : EditorWindow
{
    private ObjectField _skillField;
    private TextField _newSkillNameField;
    private SkillGraphView _graphView;
    private ActiveSkillData _skill;
    private string _newSkillName = "New Active Skill";

    public ActiveSkillData Skill => _skill;
    public string NewSkillName => _newSkillName;

    [MenuItem("Tools/Skill Graph Editor")]
    public static void Open()
    {
        var window = GetWindow<SkillEditorWindow>("Skill Graph Editor");
        window.minSize = new Vector2(960f, 560f);
    }

    private void OnEnable()
    {
        if (Selection.activeObject is ActiveSkillData activeSkill)
        {
            _skill = activeSkill;
        }
    }

    private void OnDisable()
    {
        if (_graphView != null)
        {
            _graphView.Dispose();
            _graphView = null;
        }
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is ActiveSkillData activeSkill)
        {
            SetSkill(activeSkill);
        }
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        rootVisualElement.Add(CreateToolbar());

        var body = new VisualElement
        {
            style =
            {
                flexGrow = 1f,
                flexDirection = FlexDirection.Row
            }
        };

        body.Add(CreatePalette());

        _graphView = new SkillGraphView(this)
        {
            style =
            {
                flexGrow = 1f
            }
        };
        body.Add(_graphView);

        rootVisualElement.Add(body);
        RefreshGraph();
    }

    public bool EnsureSkill()
    {
        if (_skill != null)
        {
            return true;
        }

        if (!EditorUtility.DisplayDialog("Create Active Skill", "Create a new ActiveSkillSO first?", "Create", "Cancel"))
        {
            return false;
        }

        CreateActiveSkill();
        return _skill != null;
    }

    public void SetSkill(ActiveSkillData skill)
    {
        if (_skill == skill)
        {
            return;
        }

        _skill = skill;
        if (_skillField != null)
        {
            _skillField.SetValueWithoutNotify(_skill);
        }

        RefreshGraph();
    }

    public void RefreshGraph()
    {
        if (_graphView != null)
        {
            _graphView.Populate(_skill);
        }
    }

    private Toolbar CreateToolbar()
    {
        var toolbar = new Toolbar();

        toolbar.Add(new ToolbarButton(CreateActiveSkill) { text = "New Active Skill" });

        _skillField = new ObjectField
        {
            objectType = typeof(ActiveSkillData),
            allowSceneObjects = false,
            value = _skill
        };
        _skillField.style.width = 240f;
        _skillField.RegisterValueChangedCallback(evt => SetSkill(evt.newValue as ActiveSkillData));
        toolbar.Add(_skillField);

        toolbar.Add(new ToolbarSpacer { flex = true });
        toolbar.Add(new ToolbarButton(SaveGraph) { text = "Save" });
        toolbar.Add(new ToolbarButton(SaveSkillAs) { text = "Save As" });
        toolbar.Add(new ToolbarButton(ClearGraph) { text = "Clear" });
        toolbar.Add(new ToolbarButton(AutoArrange) { text = "Auto Arrange" });

        return toolbar;
    }

    private VisualElement CreatePalette()
    {
        var palette = new ScrollView
        {
            style =
            {
                width = 190f,
                flexShrink = 0f,
                backgroundColor = new Color(0.12f, 0.12f, 0.13f)
            }
        };
        palette.contentContainer.style.paddingLeft = 8f;
        palette.contentContainer.style.paddingRight = 8f;
        palette.contentContainer.style.paddingTop = 8f;
        palette.contentContainer.style.paddingBottom = 8f;

        AddPaletteHeader(palette, "Nodes");
        AddPaletteHeader(palette, "Execution");
        AddPaletteButton<TargetMeleeAttack>(palette, "Single Target Melee");
        AddPaletteButton<ConeMeleeAttack>(palette, "Cone Melee");
        AddPaletteButton<ProjectileSkill>(palette, "Projectile Attack");

        AddPaletteHeader(palette, "Target");
        AddPaletteButton<Skill.Data.TargetData>(palette, "Target System");

        AddPaletteHeader(palette, "Trigger");
        AddPaletteButton<TriggerData>(palette, "Trigger System");

        AddPaletteHeader(palette, "Effect");
        AddPaletteButton<DamageEffect>(palette, "Damage Effect");
        AddPaletteButton<BuffEffect>(palette, "Buff Effect");
        AddPaletteButton<DebuffEffect>(palette, "Debuff Effect");
        AddPaletteButton<AttributeEffect>(palette, "Status Effect");

        AddPaletteHeader(palette, "Item");
        AddPaletteButton<ProjectileData>(palette, "Projectile");
        AddPaletteButton<SummonData>(palette, "Summon");

        AddPaletteHeader(palette, "VFX");
        AddPaletteButton<SkillVfxSystem>(palette, "Skill Visual");

        var spacer = new VisualElement { style = { height = 16f } };
        palette.Add(spacer);

        _newSkillNameField = new TextField("Name")
        {
            value = _newSkillName
        };
        _newSkillNameField.labelElement.style.minWidth = 38f;
        _newSkillNameField.labelElement.style.width = 38f;
        _newSkillNameField.labelElement.style.marginRight = 2f;
        _newSkillNameField.RegisterValueChangedCallback(evt => _newSkillName = evt.newValue);
        palette.Add(_newSkillNameField);

        var createButton = new Button(CreateActiveSkill)
        {
            text = "Create Active Skill"
        };
        createButton.style.height = 30f;
        createButton.style.marginTop = 6f;
        palette.Add(createButton);

        return palette;
    }

    private static void AddPaletteHeader(VisualElement parent, string title)
    {
        var label = new Label(title)
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                color = new Color(0.86f, 0.86f, 0.86f),
                marginTop = 8f,
                marginBottom = 3f
            }
        };
        parent.Add(label);
    }

    private void AddPaletteButton<T>(VisualElement parent, string label) where T : ScriptableObject
    {
        var button = new Button(() =>
        {
            if (EnsureSkill())
            {
                _graphView.CreateAndAssignNode<T>();
            }
        })
        {
            text = label
        };
        button.style.height = 24f;
        button.style.marginBottom = 2f;
        parent.Add(button);
    }

    private void CreateActiveSkill()
    {
        ActiveSkillData activeSkill = SkillGraphAssetUtility.CreateActiveSkill(_newSkillName);
        activeSkill.Name = _newSkillName;
        SkillGraphAssetUtility.MarkDirty(activeSkill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = activeSkill;
        SetSkill(activeSkill);
    }

    private void SaveGraph()
    {
        if (_skill == null)
        {
            return;
        }

        _graphView?.SaveLayout();
        SkillGraphAssetUtility.SaveGraph(_skill);
    }

    private void SaveSkillAs()
    {
        if (_skill == null)
        {
            return;
        }

        ActiveSkillData copy = SkillGraphAssetUtility.SaveSkillAs(_skill);
        if (copy != null)
        {
            Selection.activeObject = copy;
            SetSkill(copy);
        }
    }

    private void ClearGraph()
    {
        if (_skill == null)
        {
            return;
        }

        if (SkillGraphAssetUtility.ClearGraph(_skill))
        {
            RefreshGraph();
        }
    }

    private void AutoArrange()
    {
        _graphView?.AutoArrange();
    }
}
#endif
