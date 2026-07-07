#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Skill;
using Skill.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using SkillVfxSystem = Skill.Data.VFXData;

public sealed class SkillGraphView : GraphView
{
    private const string ExecutionVfxKeyMarker = "::execution-vfx";
    private const string ExecutionEffectKeyMarker = "::execution-effect::";
    private const string ExecutionEffectVfxKeyMarker = "::execution-effect-vfx::";
    private const string ExecutionEffectSummonKeyMarker = "::execution-effect-summon::";
    private const string SummonItemDataEffectKeyMarker = "::summon-effect::";
    private readonly SkillEditorWindow _window;
    private readonly Dictionary<string, Rect> _nodePositions = new Dictionary<string, Rect>();
    private readonly Dictionary<string, Rect> _assetPositions = new Dictionary<string, Rect>();
    private readonly Dictionary<string, SkillNodeView> _nodes = new Dictionary<string, SkillNodeView>();
    private readonly List<ScriptableObject> _looseAssets = new List<ScriptableObject>();
    private ActiveSkillData _skill;
    private bool _isBuilding;
    private Vector2 _lastGraphMousePosition;

    public SkillGraphView(SkillEditorWindow window)
    {
        _window = window;

        Insert(0, new GridBackground());
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new SkillGraphRectangleSelector(this));
        this.AddManipulator(new ClickSelector());

        graphViewChanged = OnGraphViewChanged;
        RegisterCallback<MouseMoveEvent>(OnMousePositionEvent, TrickleDown.TrickleDown);
        RegisterCallback<MouseDownEvent>(OnMousePositionEvent, TrickleDown.TrickleDown);
        RegisterCallback<MouseUpEvent>(OnMousePositionEvent, TrickleDown.TrickleDown);
        RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        RegisterCallback<DragPerformEvent>(OnDragPerform);

    }

    public void Dispose()
    {
        SaveLayout();
        UnregisterCallback<MouseMoveEvent>(OnMousePositionEvent, TrickleDown.TrickleDown);
        UnregisterCallback<MouseDownEvent>(OnMousePositionEvent, TrickleDown.TrickleDown);
        UnregisterCallback<MouseUpEvent>(OnMousePositionEvent, TrickleDown.TrickleDown);
        UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
        UnregisterCallback<DragPerformEvent>(OnDragPerform);
        graphViewChanged = null;
    }

    public void Populate(ActiveSkillData skill)
    {
        if (_skill != skill)
        {
            SaveLayout();
            _skill = skill;
            LoadLayout();
        }
        else
        {
            CacheCurrentNodePositions();
        }

        _isBuilding = true;
        DeleteElements(graphElements.ToList());
        _nodes.Clear();

        if (_skill == null)
        {
            _isBuilding = false;
            return;
        }

        AddNode(SkillNodeKind.ActiveSkill, "active", "Active Skill", _skill, -1, new Rect(40f, 160f, 290f, 360f), new Color(0.82f, 0.82f, 0.82f));

        if (_skill.Trigger != null)
        {
            AddNode(SkillNodeKind.Trigger, "trigger", "Trigger", _skill.Trigger, -1, new Rect(410f, 60f, 250f, 230f), new Color(0.36f, 0.84f, 0.31f));
        }

        if (_skill.Execution != null)
        {
            AddNode(SkillNodeKind.Execution, "execution", "Execution", _skill.Execution, -1, new Rect(740f, 170f, 300f, 380f), new Color(0.95f, 0.62f, 0.16f));
        }

        if (_skill.Target != null)
        {
            AddNode(SkillNodeKind.Target, "target", "Target", _skill.Target, -1, new Rect(410f, 350f, 250f, 240f), new Color(0.18f, 0.72f, 0.88f));
        }

        if (_skill.Execution != null)
        {
            AddExecutionReferenceNodes("execution", _skill.Execution);
        }

        for (int i = _looseAssets.Count - 1; i >= 0; i--)
        {
            ScriptableObject looseAsset = _looseAssets[i];
            if (looseAsset == null || IsReferencedBySkill(looseAsset))
            {
                _looseAssets.RemoveAt(i);
                continue;
            }

            AddLooseNode(looseAsset, i);
        }

        AddSummonItemDataEffectNodes();

        AddCachedReferenceEdge("execution", "Execution", "active", "Execution");
        AddCachedReferenceEdge("target", "Target", "active", "Target");
        AddCachedReferenceEdge("trigger", "Trigger", "active", "Trigger");

        if (_skill.Execution != null)
        {
            AddExecutionReferenceEdges("execution", _skill.Execution);
        }

        AddSummonItemDataEffectEdges();

        _isBuilding = false;
    }

    public void AutoArrange()
    {
        _nodePositions.Clear();
        _assetPositions.Clear();
        UpdateViewTransform(Vector3.zero, Vector3.one);
        Populate(_skill);
        SaveLayout();
    }

    public void SaveLayout()
    {
        if (_skill == null)
        {
            return;
        }

        CacheCurrentNodePositions();

        var layout = new SkillGraphLayoutData
        {
            ViewPosition = viewTransform.position,
            ViewScale = viewTransform.scale
        };

        foreach (KeyValuePair<string, Rect> position in _nodePositions)
        {
            layout.NodePositions.Add(new SkillGraphLayoutRect(position.Key, position.Value));
        }

        foreach (KeyValuePair<string, Rect> position in _assetPositions)
        {
            layout.AssetPositions.Add(new SkillGraphLayoutRect(position.Key, position.Value));
        }

        foreach (ScriptableObject looseAsset in _looseAssets)
        {
            string guid = SkillGraphAssetUtility.GetAssetGuid(looseAsset);
            if (!string.IsNullOrEmpty(guid) && !layout.LooseAssetGuids.Contains(guid))
            {
                layout.LooseAssetGuids.Add(guid);
            }
        }

        SkillGraphAssetUtility.SaveGraphLayout(_skill, layout);
    }

    public void CreateAndAssignNode<T>() where T : ScriptableObject
    {
        CreateAndAssignNode<T>(null);
    }

    private void CreateAndAssignNode<T>(Vector2? graphPosition) where T : ScriptableObject
    {
        if (_skill == null || !_window.EnsureSkill())
        {
            return;
        }

        T asset = SkillGraphAssetUtility.CreateAssetForSkill<T>(_skill, _window.NewSkillName);
        Undo.RegisterCreatedObjectUndo(asset, "Create Skill Node Asset");
        _looseAssets.Add(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (graphPosition.HasValue)
        {
            Rect position = new Rect(graphPosition.Value.x, graphPosition.Value.y, 280f, 260f);
            _nodePositions[GetLooseKey(asset)] = position;
            CacheAssetPosition(asset, position);
        }

        Populate(_skill);
        SaveLayout();
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);

        Vector2 graphPosition = _lastGraphMousePosition;
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Create Node/Execution/Single", _ => CreateAndAssignNode<SingleExecutionData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Execution/Random Multi", _ => CreateAndAssignNode<RandomMultiExecutionData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Execution/Sequence", _ => CreateAndAssignNode<SequenceHitExecutionData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Execution/Cone", _ => CreateAndAssignNode<ConeExecutionData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        
        evt.menu.AppendSeparator("Create Node/");
        evt.menu.AppendAction("Create Node/Target/Self", _ => CreateAndAssignNode<SelfTargetData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Target/NearHero", _ => CreateAndAssignNode<NearHeroTargetData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Target/AllHero", _ => CreateAndAssignNode<AllHeroTargetData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Target/NearEnemy", _ => CreateAndAssignNode<NearEnemyTargetData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Target/AllEnemy", _ => CreateAndAssignNode<AllEnemyTargetData>(graphPosition), DropdownMenuAction.AlwaysEnabled);

        evt.menu.AppendAction("Create Node/Trigger/None Trigger", _ => CreateAndAssignNode<NoneTriggerData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Trigger/HitCount Trigger", _ => CreateAndAssignNode<HitCountTriggerData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Trigger/Mana Trigger", _ => CreateAndAssignNode<ManaTriggerData>(graphPosition), DropdownMenuAction.AlwaysEnabled);

        evt.menu.AppendSeparator("Create Node/");
        evt.menu.AppendAction("Create Node/Effect/Damage", _ => CreateAndAssignNode<DamageEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Buff", _ => CreateAndAssignNode<BuffEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Timed Buff", _ => CreateAndAssignNode<BuffEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Spawn", _ => CreateAndAssignNode<SpawnEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Status/Dot", _ => CreateAndAssignNode<DotEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Status/Damage Transfer", _ => CreateAndAssignNode<DamageTransferEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Status/Decrease Armour", _ => CreateAndAssignNode<ArmorReduction>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Status/Slow", _ => CreateAndAssignNode<SlowEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Status/Stun", _ => CreateAndAssignNode<StunEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Status/KnockBack", _ => CreateAndAssignNode<KnockBack>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Effect/Gold", _ => CreateAndAssignNode<GoldEffect>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendSeparator("Create Node/");

        evt.menu.AppendAction("Create Node/Item/Projectile/Straight", _ => CreateAndAssignNode<StraightProjectileData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Item/Projectile/Homing", _ => CreateAndAssignNode<HomingProjectileData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Item/Projectile/Parabola", _ => CreateAndAssignNode<ParabolaProjectileData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Item/Summon/Attach", _ => CreateAndAssignNode<AttachSummonData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Item/Summon/Moveable", _ => CreateAndAssignNode<MoveableSummonData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Item/Flooring/Dot", _ => CreateAndAssignNode<DotFlooringData>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Create Node/Item/Flooring/Debuff", _ => CreateAndAssignNode<DebuffFlooringData>(graphPosition), DropdownMenuAction.AlwaysEnabled);

        evt.menu.AppendSeparator("Create Node/");
        evt.menu.AppendAction("Create Node/VFX/Skill Visual", _ => CreateAndAssignNode<SkillVfxSystem>(graphPosition), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Align/Auto Arrange", _ => AutoArrange(), DropdownMenuAction.AlwaysEnabled);
    }

    private void OnMousePositionEvent(MouseMoveEvent evt)
    {
        CacheMousePosition(evt.mousePosition);
    }

    private void OnMousePositionEvent(MouseDownEvent evt)
    {
        CacheMousePosition(evt.mousePosition);
    }

    private void OnMousePositionEvent(MouseUpEvent evt)
    {
        CacheMousePosition(evt.mousePosition);
    }

    private void CacheMousePosition(Vector2 mousePosition)
    {
        _lastGraphMousePosition = contentViewContainer.WorldToLocal(mousePosition);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports
            .Where(port => port.direction != startPort.direction && port.node != startPort.node && ArePortsCompatible(startPort, port))
            .ToList();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (_isBuilding)
        {
            return change;
        }

        if (change.movedElements != null)
        {
            foreach (GraphElement element in change.movedElements)
            {
                if (element is SkillNodeView nodeView)
                {
                    CacheNodePosition(nodeView);
                }
            }

            SaveLayout();
        }

        if (change.edgesToCreate != null)
        {
            var validEdges = new List<Edge>();
            foreach (Edge edge in change.edgesToCreate)
            {
                if (TryApplyEdge(edge))
                {
                    validEdges.Add(edge);
                }
            }

            change.edgesToCreate = validEdges;
            SaveLayout();
        }

        bool shouldRebuild = false;
        if (change.elementsToRemove != null)
        {
            foreach (GraphElement element in change.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    TryClearEdge(edge);
                }
                else if (element is SkillNodeView nodeView)
                {
                    RemoveNodeReference(nodeView);
                    shouldRebuild = true;
                }
            }
        }

        if (shouldRebuild)
        {
            EditorApplication.delayCall += () =>
            {
                Populate(_skill);
                SaveLayout();
            };
        }

        return change;
    }

    private void LoadLayout()
    {
        _nodePositions.Clear();
        _assetPositions.Clear();
        _looseAssets.Clear();

        SkillGraphLayoutData layout = SkillGraphAssetUtility.LoadGraphLayout(_skill);
        if (layout == null)
        {
            return;
        }

        if (layout.NodePositions != null)
        {
            for (int i = 0; i < layout.NodePositions.Count; i++)
            {
                SkillGraphLayoutRect entry = layout.NodePositions[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    _nodePositions[entry.Key] = entry.Position;
                }
            }
        }

        if (layout.AssetPositions != null)
        {
            for (int i = 0; i < layout.AssetPositions.Count; i++)
            {
                SkillGraphLayoutRect entry = layout.AssetPositions[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    _assetPositions[entry.Key] = entry.Position;
                }
            }
        }

        if (layout.LooseAssetGuids != null)
        {
            for (int i = 0; i < layout.LooseAssetGuids.Count; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(layout.LooseAssetGuids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && TryGetNodeInfo(asset, out _, out _, out _) && !_looseAssets.Contains(asset))
                {
                    _looseAssets.Add(asset);
                }
            }
        }

        Vector3 viewScale = layout.ViewScale;
        if (viewScale.x <= 0f || viewScale.y <= 0f || viewScale.z <= 0f)
        {
            viewScale = Vector3.one;
        }

        UpdateViewTransform(layout.ViewPosition, viewScale);
    }

    private SkillNodeView AddNode(SkillNodeKind kind, string key, string title, UnityEngine.Object asset, int effectIndex, Rect defaultPosition, Color accent, int effectPortCount = 0, bool isLoose = false)
    {
        Rect position = GetSavedPosition(key, asset, defaultPosition);
        var node = new SkillNodeView(_skill, kind, key, title, asset, effectIndex, effectPortCount, accent, isLoose)
        {
            OnFieldSlotAssigned = AssignFieldSlotAssetAndRefresh,
            OnAssetChanged = OnNodeAssetChanged,
            OnAssetRenameRequested = RenameNodeAsset,
            OnRefreshRequested = () => Populate(_skill),
            OnExpandedStateChanged = OnNodeExpandedStateChanged
        };
        node.SetPosition(position);
        node.InitializeLayoutRect(position);
        node.RegisterCallback<GeometryChangedEvent>(evt => OnNodeGeometryChanged(node, evt));

        AddElement(node);
        _nodes[key] = node;
        return node;
    }

    private void OnNodeExpandedStateChanged(SkillNodeView nodeView)
    {
        if (_isBuilding || nodeView == null)
        {
            return;
        }

        CacheNodePosition(nodeView);
        SaveLayout();
    }

    private void OnNodeGeometryChanged(SkillNodeView nodeView, GeometryChangedEvent evt)
    {
        if (_isBuilding || nodeView == null)
        {
            return;
        }

        if (Mathf.Approximately(evt.oldRect.width, evt.newRect.width)
            && Mathf.Approximately(evt.oldRect.height, evt.newRect.height))
        {
            return;
        }

        CacheNodePosition(nodeView);
        SaveLayout();
    }

    private void CacheCurrentNodePositions()
    {
        foreach (SkillNodeView nodeView in _nodes.Values)
        {
            CacheNodePosition(nodeView);
        }
    }

    private void CacheNodePosition(SkillNodeView nodeView)
    {
        if (nodeView == null)
        {
            return;
        }

        Rect position = nodeView.GetLayoutRect();
        _nodePositions[nodeView.Key] = position;
        CacheAssetPosition(nodeView.Asset, position);
    }

    private void CacheAssetPosition(UnityEngine.Object asset, Rect position)
    {
        string assetKey = GetAssetPositionKey(asset);
        if (!string.IsNullOrEmpty(assetKey))
        {
            _assetPositions[assetKey] = position;
        }
    }

    private Rect GetSavedPosition(string key, UnityEngine.Object asset, Rect defaultPosition)
    {
        if (_nodePositions.TryGetValue(key, out Rect savedPosition))
        {
            return savedPosition;
        }

        string assetKey = GetAssetPositionKey(asset);
        if (!string.IsNullOrEmpty(assetKey) && _assetPositions.TryGetValue(assetKey, out Rect savedAssetPosition))
        {
            _nodePositions[key] = savedAssetPosition;
            return savedAssetPosition;
        }

        return defaultPosition;
    }

    private void OnDragUpdated(DragUpdatedEvent evt)
    {
        if (!HasSupportedDraggedAsset())
        {
            return;
        }

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        evt.StopPropagation();
    }

    private void OnDragPerform(DragPerformEvent evt)
    {
        if (!HasSupportedDraggedAsset())
        {
            return;
        }

        DragAndDrop.AcceptDrag();

        Vector2 graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        int addedCount = 0;
        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
        {
            if (!(draggedObject is ScriptableObject asset) || !CanAcceptDraggedAsset(asset))
            {
                continue;
            }

            Vector2 offsetPosition = graphPosition + new Vector2(28f * addedCount, 28f * addedCount);
            if (AddDraggedAsset(asset, offsetPosition))
            {
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            Populate(_skill);
            SaveLayout();
        }

        evt.StopPropagation();
    }

    private bool HasSupportedDraggedAsset()
    {
        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
        {
            if (draggedObject is ScriptableObject asset && CanAcceptDraggedAsset(asset))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanAcceptDraggedAsset(ScriptableObject asset)
    {
        if (asset is ActiveSkillData)
        {
            return true;
        }

        return _skill != null && TryGetNodeInfo(asset, out _, out _, out _);
    }

    private bool AddDraggedAsset(ScriptableObject asset, Vector2 graphPosition)
    {
        if (asset is ActiveSkillData activeSkill)
        {
            _window.SetSkill(activeSkill);
            return true;
        }

        if (_skill == null || !TryGetNodeInfo(asset, out _, out _, out _))
        {
            return false;
        }

        string key = GetNodeKeyForAsset(asset);
        _nodePositions[key] = new Rect(graphPosition.x, graphPosition.y, 280f, 260f);

        if (!IsReferencedBySkill(asset) && !_looseAssets.Contains(asset))
        {
            _looseAssets.Add(asset);
        }

        return true;
    }

    private string GetNodeKeyForAsset(UnityEngine.Object asset)
    {
        if (_skill == null || asset == null)
        {
            return GetLooseKey(asset);
        }

        if (_skill.Execution == asset)
        {
            return "execution";
        }

        if (_skill.Target == asset)
        {
            return "target";
        }

        if (_skill.Trigger == asset)
        {
            return "trigger";
        }

        if (_skill.Execution != null)
        {
            string executionNodeKey = GetExecutionNodeKeyForAsset("execution", _skill.Execution, asset);
            if (!string.IsNullOrEmpty(executionNodeKey))
            {
                return executionNodeKey;
            }
        }

        string summonEffectKey = GetSummonItemDataEffectNodeKeyForAsset(asset);
        if (!string.IsNullOrEmpty(summonEffectKey))
        {
            return summonEffectKey;
        }

        return GetLooseKey(asset);
    }

    private string GetExecutionNodeKeyForAsset(string executionNodeKey, ExecutionData execution, UnityEngine.Object asset)
    {
        if (execution == null || asset == null)
        {
            return string.Empty;
        }

        if (execution == asset)
        {
            return executionNodeKey;
        }

        if (execution.VFX == asset)
        {
            return GetExecutionVfxNodeKey(executionNodeKey);
        }

        if (execution.Effects == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < execution.Effects.Count; i++)
        {
            EffectBase entry = execution.Effects[i];
            if (entry?.VFX == asset)
            {
                return GetExecutionEffectVfxNodeKey(executionNodeKey, i);
            }

            if (entry is SpawnEffect spawnEffect && spawnEffect.Item == asset)
            {
                return GetExecutionEffectSummonNodeKey(executionNodeKey, i);
            }

            if (entry != null && entry == asset)
            {
                return GetExecutionEffectNodeKey(executionNodeKey, i);
            }
        }

        return string.Empty;
    }

    private string GetSummonItemDataEffectNodeKeyForAsset(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return string.Empty;
        }

        foreach (SkillNodeView node in _nodes.Values)
        {
            if (node.Kind == SkillNodeKind.SummonItemData
                && node.Asset is SummonItemData summonData
                && TryGetSummonItemDataEffectIndex(summonData, asset, out int effectIndex))
            {
                return GetSummonItemDataEffectNodeKey(node.Key, effectIndex);
            }
        }

        for (int i = 0; i < _looseAssets.Count; i++)
        {
            if (_looseAssets[i] is SummonItemData summonData
                && TryGetSummonItemDataEffectIndex(summonData, asset, out int effectIndex))
            {
                return GetSummonItemDataEffectNodeKey(GetLooseKey(summonData), effectIndex);
            }
        }

        return string.Empty;
    }

    private static bool TryGetSummonItemDataEffectIndex(SummonItemData summonData, UnityEngine.Object asset, out int effectIndex)
    {
        effectIndex = -1;
        if (summonData?.Effects == null || asset == null)
        {
            return false;
        }

        for (int i = 0; i < summonData.Effects.Count; i++)
        {
            if (summonData.Effects[i] == asset)
            {
                effectIndex = i;
                return true;
            }
        }

        return false;
    }

    private void AddCachedReferenceEdge(string sourceNodeKey, string sourcePortName, string targetNodeKey, string targetPortName)
    {
        if (!_nodes.TryGetValue(sourceNodeKey, out SkillNodeView sourceNode) || !_nodes.TryGetValue(targetNodeKey, out SkillNodeView targetNode))
        {
            return;
        }

        Port outputPort = sourceNode.GetOutputPort(sourcePortName);
        Port inputPort = targetNode.GetInputPort(targetPortName);
        if (outputPort == null || inputPort == null)
        {
            return;
        }

        var edge = outputPort.ConnectTo(inputPort);
        AddElement(edge);
    }

    private void AddExecutionReferenceNodes(string executionNodeKey, ExecutionData execution)
    {
        if (execution == null || !_nodes.TryGetValue(executionNodeKey, out SkillNodeView executionNode))
        {
            return;
        }

        Rect executionPosition = executionNode.GetPosition();

        if (execution.VFX != null)
        {
            AddNode(SkillNodeKind.ExecutionVfx, GetExecutionVfxNodeKey(executionNodeKey), "VFX", execution.VFX, -1, new Rect(executionPosition.x + 370f, executionPosition.y - 90f, 250f, 260f), new Color(0.66f, 0.38f, 0.92f));
        }

        if (execution.Effects == null)
        {
            return;
        }

        for (int i = 0; i < execution.Effects.Count; i++)
        {
            EffectBase entry = execution.Effects[i];
            if (entry == null)
            {
                continue;
            }

            AddNode(SkillNodeKind.Effect, GetExecutionEffectNodeKey(executionNodeKey, i), "Effect (" + (i + 1) + ")", entry, i, new Rect(executionPosition.x + 370f, executionPosition.y + 220f + i * 290f, 270f, 270f), new Color(0.92f, 0.28f, 0.14f));
            if (entry.VFX != null)
            {
                AddNode(SkillNodeKind.ExecutionVfx, GetExecutionEffectVfxNodeKey(executionNodeKey, i), "Effect VFX (" + (i + 1) + ")", entry.VFX, i, new Rect(executionPosition.x + 690f, executionPosition.y + 220f + i * 290f, 250f, 260f), new Color(0.66f, 0.38f, 0.92f));
            }

            if (entry is SpawnEffect spawnEffect
                && spawnEffect.Item != null
                && TryGetNodeInfo(spawnEffect.Item, out SkillNodeKind itemKind, out _, out Color itemColor))
            {
                AddNode(itemKind, GetExecutionEffectSummonNodeKey(executionNodeKey, i), "Spawn Item (" + (i + 1) + ")", spawnEffect.Item, i, new Rect(executionPosition.x + 1010f, executionPosition.y + 220f + i * 290f, 270f, 260f), itemColor);
            }
        }
    }

    private void AddExecutionReferenceEdges(string executionNodeKey, ExecutionData execution)
    {
        if (execution == null)
        {
            return;
        }

        AddCachedReferenceEdge(GetExecutionVfxNodeKey(executionNodeKey), "VFX", executionNodeKey, "VFX");

        if (execution.Effects == null)
        {
            return;
        }

        for (int i = 0; i < execution.Effects.Count; i++)
        {
            if (execution.Effects[i] == null)
            {
                continue;
            }

            string effectNodeKey = GetExecutionEffectNodeKey(executionNodeKey, i);
            AddCachedReferenceEdge(effectNodeKey, "Effect", executionNodeKey, SkillNodeView.GetEffectSlotName(i));
            if (execution.Effects[i].VFX != null)
            {
                AddCachedReferenceEdge(GetExecutionEffectVfxNodeKey(executionNodeKey, i), "VFX", effectNodeKey, "VFX");
            }

            if (execution.Effects[i] is SpawnEffect spawnEffect && spawnEffect.Item != null)
            {
                AddCachedReferenceEdge(GetExecutionEffectSummonNodeKey(executionNodeKey, i), "Item", effectNodeKey, "Item");
            }
        }
    }

    private void AddSummonItemDataEffectNodes()
    {
        List<SkillNodeView> summonNodes = _nodes.Values
            .Where(node => node.Kind == SkillNodeKind.SummonItemData && node.Asset is SummonItemData)
            .ToList();

        for (int nodeIndex = 0; nodeIndex < summonNodes.Count; nodeIndex++)
        {
            SkillNodeView summonNode = summonNodes[nodeIndex];
            var summonData = summonNode.Asset as SummonItemData;
            if (summonData?.Effects == null)
            {
                continue;
            }

            Rect summonPosition = summonNode.GetPosition();
            for (int i = 0; i < summonData.Effects.Count; i++)
            {
                EffectBase entry = summonData.Effects[i];
                if (entry == null)
                {
                    continue;
                }

                string key = GetSummonItemDataEffectNodeKey(summonNode.Key, i);
                Rect defaultPosition = new Rect(summonPosition.x + 330f, summonPosition.y + 90f + i * 290f, 270f, 270f);
                AddNode(SkillNodeKind.Effect, key, "Summon Effect (" + (i + 1) + ")", entry, i, defaultPosition, new Color(0.92f, 0.28f, 0.14f));
            }
        }
    }

    private void AddSummonItemDataEffectEdges()
    {
        List<SkillNodeView> summonNodes = _nodes.Values
            .Where(node => node.Kind == SkillNodeKind.SummonItemData && node.Asset is SummonItemData)
            .ToList();

        for (int nodeIndex = 0; nodeIndex < summonNodes.Count; nodeIndex++)
        {
            SkillNodeView summonNode = summonNodes[nodeIndex];
            var summonData = summonNode.Asset as SummonItemData;
            if (summonData?.Effects == null)
            {
                continue;
            }

            for (int i = 0; i < summonData.Effects.Count; i++)
            {
                if (summonData.Effects[i] == null)
                {
                    continue;
                }

                AddCachedReferenceEdge(GetSummonItemDataEffectNodeKey(summonNode.Key, i), "Effect", summonNode.Key, SkillNodeView.GetEffectSlotName(i));
            }
        }
    }

    private void AddLooseNode(ScriptableObject asset, int index)
    {
        if (!TryGetNodeInfo(asset, out SkillNodeKind kind, out string title, out Color color))
        {
            return;
        }

        string key = GetLooseKey(asset);
        Rect defaultPosition = new Rect(420f + index * 35f, 90f + index * 35f, 280f, 260f);
        AddNode(kind, key, title, asset, -1, defaultPosition, color, 0, true);
    }

    private static bool TryGetNodeInfo(ScriptableObject asset, out SkillNodeKind kind, out string title, out Color color)
    {
        if (asset is ExecutionData)
        {
            kind = SkillNodeKind.Execution;
            title = "Execution";
            color = new Color(0.95f, 0.62f, 0.16f);
            return true;
        }

        if (asset is Skill.Data.TargetData)
        {
            kind = SkillNodeKind.Target;
            title = "Target";
            color = new Color(0.18f, 0.72f, 0.88f);
            return true;
        }

        if (asset is TriggerData)
        {
            kind = SkillNodeKind.Trigger;
            title = "Trigger";
            color = new Color(0.36f, 0.84f, 0.31f);
            return true;
        }

        if (asset is SkillVfxSystem)
        {
            kind = SkillNodeKind.ExecutionVfx;
            title = "VFX";
            color = new Color(0.66f, 0.38f, 0.92f);
            return true;
        }

        if (asset is EffectBase)
        {
            kind = SkillNodeKind.Effect;
            title = "Effect";
            color = new Color(0.92f, 0.28f, 0.14f);
            return true;
        }

        if (asset is ProjectileDataBase)
        {
            kind = SkillNodeKind.ProjectileData;
            title = "Projectile";
            color = new Color(0.95f, 0.82f, 0.18f);
            return true;
        }

        if (asset is SummonItemData)
        {
            kind = SkillNodeKind.SummonItemData;
            title = "Summon";
            color = new Color(0.34f, 0.76f, 0.92f);
            return true;
        }

        kind = default;
        title = string.Empty;
        color = Color.white;
        return false;
    }

    private static bool ArePortsCompatible(Port firstPort, Port secondPort)
    {
        Port inputPort = firstPort.direction == Direction.Input ? firstPort : secondPort;
        Port outputPort = firstPort.direction == Direction.Output ? firstPort : secondPort;

        if (!(inputPort.userData is SkillNodePortData inputData) || string.IsNullOrEmpty(inputData.SlotName))
        {
            return false;
        }

        if (!(outputPort.node is SkillNodeView outputNode) || outputNode.Asset == null)
        {
            return false;
        }

        return inputData.ValueType.IsAssignableFrom(outputNode.Asset.GetType());
    }

    private bool TryApplyEdge(Edge edge)
    {
        if (edge?.input == null || edge.output == null)
        {
            return false;
        }

        if (!(edge.input.userData is SkillNodePortData inputData) || string.IsNullOrEmpty(inputData.SlotName))
        {
            return false;
        }

        if (!(edge.input.node is SkillNodeView inputNode) || !(edge.output.node is SkillNodeView outputNode))
        {
            return false;
        }

        if (outputNode.Asset == null)
        {
            return false;
        }

        if (!inputData.ValueType.IsAssignableFrom(outputNode.Asset.GetType()))
        {
            return false;
        }

        CacheAssignedNodePosition(inputNode, inputData.SlotName, outputNode);
        AssignFieldSlotAsset(inputNode, inputData.SlotName, outputNode.Asset);
        inputNode.SetFieldValueWithoutNotify(inputData.SlotName, outputNode.Asset);

        if (outputNode.IsLoose && outputNode.Asset is ScriptableObject looseAsset && IsReferencedBySkill(looseAsset))
        {
            _looseAssets.Remove(looseAsset);
        }

        return true;
    }

    private void TryClearEdge(Edge edge)
    {
        if (edge?.input == null || edge.output == null)
        {
            return;
        }

        if (!(edge.input.userData is SkillNodePortData inputData) || string.IsNullOrEmpty(inputData.SlotName))
        {
            return;
        }

        if (!(edge.input.node is SkillNodeView inputNode) || !(edge.output.node is SkillNodeView outputNode))
        {
            return;
        }

        if (IsFieldSlotAssignedTo(inputNode, inputData.SlotName, outputNode.Asset))
        {
            ClearFieldSlotAsset(inputNode, inputData.SlotName, outputNode.Asset);
            inputNode.SetFieldValueWithoutNotify(inputData.SlotName, null);
        }
    }

    private void AssignFieldSlotAsset(SkillNodeView nodeView, string slotName, UnityEngine.Object newAsset)
    {
        if (_skill == null)
        {
            return;
        }

        if (nodeView.Kind == SkillNodeKind.ActiveSkill)
        {
            Undo.RecordObject(_skill, "Assign Active Skill Slot");
            switch (slotName)
            {
                case "Execution":
                    _skill.Execution = newAsset as ExecutionData;
                    break;
                case "Target":
                    _skill.Target = newAsset as TargetData;
                    break;
                case "Trigger":
                    _skill.Trigger = newAsset as TriggerData;
                    break;
            }

            SkillGraphAssetUtility.MarkDirty(_skill);
            return;
        }

        if (nodeView.Kind == SkillNodeKind.Execution && nodeView.Asset is ExecutionData execution)
        {
            Undo.RecordObject(execution, "Assign Execution Slot");
            if (TryGetEffectSlotIndex(slotName, out int effectIndex))
            {
                if (newAsset is EffectBase indexedEffect)
                {
                    SetEffectAt(execution, effectIndex, indexedEffect);
                }

                SkillGraphAssetUtility.MarkDirty(execution);
                return;
            }

            switch (slotName)
            {
                case "VFX":
                    execution.VFX = newAsset as SkillVfxSystem;
                    break;
            }

            SkillGraphAssetUtility.MarkDirty(execution);
            return;
        }

        if (nodeView.Kind == SkillNodeKind.SummonItemData && nodeView.Asset is SummonItemData summonData)
        {
            Undo.RecordObject(summonData, "Assign Summon Slot");
            if (TryGetEffectSlotIndex(slotName, out int effectIndex))
            {
                if (newAsset is EffectBase indexedEffect)
                {
                    SetEffectAt(summonData, effectIndex, indexedEffect);
                }

                SkillGraphAssetUtility.MarkDirty(summonData);
                return;
            }
        }

        if (nodeView.Kind == SkillNodeKind.Effect && nodeView.Asset is EffectBase effectAsset)
        {
            Undo.RecordObject(effectAsset, "Assign Effect Slot");
            switch (slotName)
            {
                case "VFX":
                    effectAsset.VFX = newAsset as SkillVfxSystem;
                    break;
                case "Item":
                    if (effectAsset is SpawnEffect spawnEffect)
                    {
                        spawnEffect.Item = newAsset as SpawnItemData;
                    }
                    break;
            }

            SkillGraphAssetUtility.MarkDirty(effectAsset);
        }
    }

    private void ClearFieldSlotAsset(SkillNodeView nodeView, string slotName, UnityEngine.Object oldAsset)
    {
        if (_skill == null)
        {
            return;
        }

        if (nodeView.Kind == SkillNodeKind.ActiveSkill)
        {
            Undo.RecordObject(_skill, "Clear Active Skill Slot");
            switch (slotName)
            {
                case "Execution":
                    if (_skill.Execution == oldAsset)
                    {
                        _skill.Execution = null;
                    }
                    break;
                case "Target":
                    if (_skill.Target == oldAsset)
                    {
                        _skill.Target = null;
                    }
                    break;
                case "Trigger":
                    if (_skill.Trigger == oldAsset)
                    {
                        _skill.Trigger = null;
                    }
                    break;
            }

            SkillGraphAssetUtility.MarkDirty(_skill);
            return;
        }

        if (nodeView.Kind == SkillNodeKind.Execution && nodeView.Asset is ExecutionData execution)
        {
            Undo.RecordObject(execution, "Clear Execution Slot");
            if (TryGetEffectSlotIndex(slotName, out int effectIndex))
            {
                if (execution.Effects != null
                    && effectIndex >= 0
                    && effectIndex < execution.Effects.Count
                    && execution.Effects[effectIndex] == oldAsset)
                {
                    execution.Effects[effectIndex] = null;
                }

                SkillGraphAssetUtility.MarkDirty(execution);
                return;
            }

            switch (slotName)
            {
                case "VFX":
                    if (execution.VFX == oldAsset)
                    {
                        execution.VFX = null;
                    }
                    break;
            }

            SkillGraphAssetUtility.MarkDirty(execution);
            return;
        }

        if (nodeView.Kind == SkillNodeKind.SummonItemData && nodeView.Asset is SummonItemData summonData)
        {
            Undo.RecordObject(summonData, "Clear Summon Slot");
            if (TryGetEffectSlotIndex(slotName, out int effectIndex))
            {
                if (summonData.Effects != null
                    && effectIndex >= 0
                    && effectIndex < summonData.Effects.Count
                    && summonData.Effects[effectIndex] == oldAsset)
                {
                    summonData.Effects[effectIndex] = null;
                }

                SkillGraphAssetUtility.MarkDirty(summonData);
                return;
            }
        }

        if (nodeView.Kind == SkillNodeKind.Effect && nodeView.Asset is EffectBase effectAsset)
        {
            Undo.RecordObject(effectAsset, "Clear Effect Slot");
            switch (slotName)
            {
                case "VFX":
                    if (effectAsset.VFX == oldAsset)
                    {
                        effectAsset.VFX = null;
                    }
                    break;
                case "Item":
                    if (effectAsset is SpawnEffect spawnEffect && spawnEffect.Item == oldAsset)
                    {
                        spawnEffect.Item = null;
                    }
                    break;
            }

            SkillGraphAssetUtility.MarkDirty(effectAsset);
        }


    }

    private static void SetEffectAt(ExecutionData execution, int index, EffectBase effect)
    {
        if (execution.Effects == null)
        {
            execution.Effects = new List<EffectBase>();
        }

        while (execution.Effects.Count <= index)
        {
            execution.Effects.Add(null);
        }

        execution.Effects[index] = effect;
    }

    private static void SetEffectAt(SummonItemData summonData, int index, EffectBase effect)
    {
        if (summonData.Effects == null)
        {
            summonData.Effects = new List<EffectBase>();
        }

        while (summonData.Effects.Count <= index)
        {
            summonData.Effects.Add(null);
        }

        summonData.Effects[index] = effect;
    }

    private void CacheAssignedNodePosition(SkillNodeView inputNode, string slotName, SkillNodeView outputNode)
    {
        if (outputNode == null)
        {
            return;
        }

        Rect position = outputNode.GetPosition();
        CacheAssetPosition(outputNode.Asset, position);

        string nodeKey = GetSlotNodeKey(inputNode, slotName);
        if (!string.IsNullOrEmpty(nodeKey))
        {
            _nodePositions[nodeKey] = position;
        }
    }

    private static string GetSlotNodeKey(SkillNodeView inputNode, string slotName)
    {
        if (inputNode != null && inputNode.Kind == SkillNodeKind.Effect && slotName == "VFX" && inputNode.EffectIndex >= 0)
        {
            return "effect-vfx-" + inputNode.EffectIndex;
        }

        if (inputNode != null && inputNode.Kind == SkillNodeKind.Effect && slotName == "Item" && inputNode.EffectIndex >= 0)
        {
            return "effect-summon-" + inputNode.EffectIndex;
        }

        if (inputNode != null && inputNode.Kind == SkillNodeKind.SummonItemData && TryGetEffectSlotIndex(slotName, out int summonEffectIndex))
        {
            return GetSummonItemDataEffectNodeKey(inputNode.Key, summonEffectIndex);
        }

        if (inputNode != null && inputNode.Kind == SkillNodeKind.Execution && TryGetEffectSlotIndex(slotName, out int effectIndex))
        {
            return GetExecutionEffectNodeKey(inputNode.Key, effectIndex);
        }

        if (inputNode != null && inputNode.Kind == SkillNodeKind.Execution)
        {
            switch (slotName)
            {
                case "VFX":
                    return GetExecutionVfxNodeKey(inputNode.Key);
            }
        }

        switch (slotName)
        {
            case "Execution":
                return "execution";
            case "Target":
                return "target";
            case "Trigger":
                return "trigger";
            default:
                return string.Empty;
        }
    }

    private static string GetExecutionVfxNodeKey(string executionNodeKey)
    {
        return executionNodeKey == "execution" ? "execution-vfx" : executionNodeKey + ExecutionVfxKeyMarker;
    }

    private static string GetExecutionEffectNodeKey(string executionNodeKey, int effectIndex)
    {
        return executionNodeKey == "execution" ? "effect-" + effectIndex : executionNodeKey + ExecutionEffectKeyMarker + effectIndex;
    }

    private static string GetExecutionEffectVfxNodeKey(string executionNodeKey, int effectIndex)
    {
        return executionNodeKey == "execution" ? "effect-vfx-" + effectIndex : executionNodeKey + ExecutionEffectVfxKeyMarker + effectIndex;
    }

    private static string GetExecutionEffectSummonNodeKey(string executionNodeKey, int effectIndex)
    {
        return executionNodeKey == "execution" ? "effect-summon-" + effectIndex : executionNodeKey + ExecutionEffectSummonKeyMarker + effectIndex;
    }

    private static string GetSummonItemDataEffectNodeKey(string summonNodeKey, int effectIndex)
    {
        return summonNodeKey + SummonItemDataEffectKeyMarker + effectIndex;
    }

    private static bool TryGetSummonItemDataEffectNodeInfo(SkillNodeView nodeView, out string summonNodeKey, out int effectIndex)
    {
        summonNodeKey = string.Empty;
        effectIndex = -1;
        if (nodeView == null || string.IsNullOrEmpty(nodeView.Key))
        {
            return false;
        }

        int markerIndex = nodeView.Key.LastIndexOf(SummonItemDataEffectKeyMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        string indexText = nodeView.Key.Substring(markerIndex + SummonItemDataEffectKeyMarker.Length);
        if (!int.TryParse(indexText, out effectIndex) || effectIndex < 0)
        {
            effectIndex = -1;
            return false;
        }

        summonNodeKey = nodeView.Key.Substring(0, markerIndex);
        return !string.IsNullOrEmpty(summonNodeKey);
    }

    private static bool TryGetExecutionVfxNodeInfo(SkillNodeView nodeView, out string ownerExecutionNodeKey)
    {
        ownerExecutionNodeKey = string.Empty;
        if (nodeView == null || string.IsNullOrEmpty(nodeView.Key))
        {
            return false;
        }

        if (nodeView.Key == "execution-vfx")
        {
            ownerExecutionNodeKey = "execution";
            return true;
        }

        int markerIndex = nodeView.Key.LastIndexOf(ExecutionVfxKeyMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        ownerExecutionNodeKey = nodeView.Key.Substring(0, markerIndex);
        return !string.IsNullOrEmpty(ownerExecutionNodeKey);
    }

    private static bool TryGetExecutionEffectNodeInfo(SkillNodeView nodeView, out string ownerExecutionNodeKey, out int effectIndex)
    {
        ownerExecutionNodeKey = string.Empty;
        effectIndex = -1;
        if (nodeView == null || string.IsNullOrEmpty(nodeView.Key))
        {
            return false;
        }

        if (nodeView.Key.StartsWith("effect-", StringComparison.Ordinal)
            && !nodeView.Key.StartsWith("effect-vfx-", StringComparison.Ordinal)
            && !nodeView.Key.StartsWith("effect-summon-", StringComparison.Ordinal))
        {
            string rootIndexText = nodeView.Key.Substring("effect-".Length);
            if (int.TryParse(rootIndexText, out effectIndex) && effectIndex >= 0)
            {
                ownerExecutionNodeKey = "execution";
                return true;
            }

            effectIndex = -1;
            return false;
        }

        return TryGetExecutionIndexedNodeInfo(nodeView.Key, ExecutionEffectKeyMarker, out ownerExecutionNodeKey, out effectIndex);
    }

    private static bool TryGetExecutionEffectVfxNodeInfo(SkillNodeView nodeView, out string ownerExecutionNodeKey, out int effectIndex)
    {
        ownerExecutionNodeKey = string.Empty;
        effectIndex = -1;
        if (nodeView == null || string.IsNullOrEmpty(nodeView.Key))
        {
            return false;
        }

        if (nodeView.Key.StartsWith("effect-vfx-", StringComparison.Ordinal))
        {
            string rootIndexText = nodeView.Key.Substring("effect-vfx-".Length);
            if (int.TryParse(rootIndexText, out effectIndex) && effectIndex >= 0)
            {
                ownerExecutionNodeKey = "execution";
                return true;
            }

            effectIndex = -1;
            return false;
        }

        return TryGetExecutionIndexedNodeInfo(nodeView.Key, ExecutionEffectVfxKeyMarker, out ownerExecutionNodeKey, out effectIndex);
    }

    private static bool TryGetExecutionEffectSummonNodeInfo(SkillNodeView nodeView, out string ownerExecutionNodeKey, out int effectIndex)
    {
        ownerExecutionNodeKey = string.Empty;
        effectIndex = -1;
        if (nodeView == null || string.IsNullOrEmpty(nodeView.Key))
        {
            return false;
        }

        if (nodeView.Key.StartsWith("effect-summon-", StringComparison.Ordinal))
        {
            string rootIndexText = nodeView.Key.Substring("effect-summon-".Length);
            if (int.TryParse(rootIndexText, out effectIndex) && effectIndex >= 0)
            {
                ownerExecutionNodeKey = "execution";
                return true;
            }

            effectIndex = -1;
            return false;
        }

        return TryGetExecutionIndexedNodeInfo(nodeView.Key, ExecutionEffectSummonKeyMarker, out ownerExecutionNodeKey, out effectIndex);
    }

    private static bool TryGetExecutionIndexedNodeInfo(string nodeKey, string marker, out string ownerExecutionNodeKey, out int effectIndex)
    {
        ownerExecutionNodeKey = string.Empty;
        effectIndex = -1;

        int markerIndex = nodeKey.LastIndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        string indexText = nodeKey.Substring(markerIndex + marker.Length);
        if (!int.TryParse(indexText, out effectIndex) || effectIndex < 0)
        {
            effectIndex = -1;
            return false;
        }

        ownerExecutionNodeKey = nodeKey.Substring(0, markerIndex);
        return !string.IsNullOrEmpty(ownerExecutionNodeKey);
    }

    private void AssignFieldSlotAssetAndRefresh(SkillNodeView nodeView, string slotName, UnityEngine.Object newAsset)
    {
        AssignFieldSlotAsset(nodeView, slotName, newAsset);
        Populate(_skill);
    }

    private bool IsFieldSlotAssignedTo(SkillNodeView nodeView, string slotName, UnityEngine.Object asset)
    {
        if (_skill == null)
        {
            return false;
        }

        if (nodeView.Kind == SkillNodeKind.ActiveSkill)
        {
            switch (slotName)
            {
                case "Execution":
                    return _skill.Execution == asset;
                case "Target":
                    return _skill.Target == asset;
                case "Trigger":
                    return _skill.Trigger == asset;
                default:
                    return false;
            }
        }

        if (nodeView.Kind == SkillNodeKind.Execution && nodeView.Asset is ExecutionData execution)
        {
            if (TryGetEffectSlotIndex(slotName, out int effectIndex))
            {
                return execution.Effects != null
                    && effectIndex >= 0
                    && effectIndex < execution.Effects.Count
                    && execution.Effects[effectIndex] != null
                    && execution.Effects[effectIndex] == asset;
            }

            switch (slotName)
            {
                case "VFX":
                    return execution.VFX == asset;
                default:
                    return false;
            }
        }

        if (nodeView.Kind == SkillNodeKind.SummonItemData && nodeView.Asset is SummonItemData summonData)
        {
            if (TryGetEffectSlotIndex(slotName, out int effectIndex))
            {
                return summonData.Effects != null
                    && effectIndex >= 0
                    && effectIndex < summonData.Effects.Count
                    && summonData.Effects[effectIndex] != null
                    && summonData.Effects[effectIndex] == asset;
            }

            return false;
        }

        if (nodeView.Kind == SkillNodeKind.Effect && nodeView.Asset is EffectBase effectAsset)
        {
            switch (slotName)
            {
                case "VFX":
                    return effectAsset.VFX == asset;
                case "Item":
                    return effectAsset is SpawnEffect spawnEffect && spawnEffect.Item == asset;
                default:
                    return false;
            }
        }

        return false;
    }

    private static bool TryGetEffectSlotIndex(string slotName, out int index)
    {
        index = -1;
        const string prefix = "Effects[";
        if (string.IsNullOrEmpty(slotName) || !slotName.StartsWith(prefix) || !slotName.EndsWith("]"))
        {
            return false;
        }

        string indexText = slotName.Substring(prefix.Length, slotName.Length - prefix.Length - 1);
        return int.TryParse(indexText, out index) && index >= 0;
    }

    private bool IsReferencedBySkill(UnityEngine.Object asset)
    {
        if (_skill == null || asset == null)
        {
            return false;
        }

        if (_skill.Execution == asset || _skill.Target == asset || _skill.Trigger == asset)
        {
            return true;
        }

        if (IsReferencedBySummonItemDataEffects(asset))
        {
            return true;
        }

        return IsReferencedByExecutionGraph(_skill.Execution, asset, new HashSet<SummonItemData>());
    }

    private static bool IsReferencedByExecutionGraph(ExecutionData execution, UnityEngine.Object asset, HashSet<SummonItemData> visitedSummons)
    {
        if (execution == null || asset == null)
        {
            return false;
        }

        if (execution == asset || execution.VFX == asset)
        {
            return true;
        }

        if (execution.Effects == null)
        {
            return false;
        }

        for (int i = 0; i < execution.Effects.Count; i++)
        {
            EffectBase entry = execution.Effects[i];
            if (entry == asset || entry?.VFX == asset)
            {
                return true;
            }

            if (entry is SpawnEffect spawnEffect)
            {
                if (spawnEffect.Item == asset
                    || (spawnEffect.Item is SummonItemData summonItem && IsReferencedBySummonItemDataEffects(summonItem, asset, visitedSummons)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsReferencedBySummonItemDataEffects(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return false;
        }

        var visited = new HashSet<SummonItemData>();
        foreach (SkillNodeView node in _nodes.Values)
        {
            if (node.Asset is SummonItemData summonData && IsReferencedBySummonItemDataEffects(summonData, asset, visited))
            {
                return true;
            }
        }

        for (int i = 0; i < _looseAssets.Count; i++)
        {
            if (_looseAssets[i] is SummonItemData summonData && IsReferencedBySummonItemDataEffects(summonData, asset, visited))
            {
                return true;
            }
        }

        ExecutionData execution = _skill?.Execution;
        if (execution?.Effects != null)
        {
            for (int i = 0; i < execution.Effects.Count; i++)
            {
                if (execution.Effects[i] is SpawnEffect spawnEffect
                    && spawnEffect.Item is SummonItemData summonItem
                    && IsReferencedBySummonItemDataEffects(summonItem, asset, visited))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsReferencedBySummonItemDataEffects(SummonItemData summonData, UnityEngine.Object asset, HashSet<SummonItemData> visited)
    {
        if (summonData == null || !visited.Add(summonData))
        {
            return false;
        }

        if (summonData.Effects == null)
        {
            return false;
        }

        for (int i = 0; i < summonData.Effects.Count; i++)
        {
            if (summonData.Effects[i] == asset)
            {
                return true;
            }

            if (summonData.Effects[i] is SpawnEffect spawnEffect
                && spawnEffect.Item is SummonItemData summonItem
                && IsReferencedBySummonItemDataEffects(summonItem, asset, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetLooseKey(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return "loose-null";
        }

        string path = AssetDatabase.GetAssetPath(asset);
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (!string.IsNullOrEmpty(guid))
        {
            return "loose-" + guid;
        }

        return "loose-" + asset.GetInstanceID();
    }

    private static string GetAssetPositionKey(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return string.Empty;
        }

        string path = AssetDatabase.GetAssetPath(asset);
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (!string.IsNullOrEmpty(guid))
        {
            return "asset-" + guid;
        }

        return "asset-" + asset.GetInstanceID();
    }

    private void OnNodeAssetChanged(UnityEngine.Object changedAsset, bool structureChanged)
    {
        SkillGraphAssetUtility.MarkDirty(changedAsset);
        if (structureChanged)
        {
            Populate(_skill);
        }
    }

    private void RenameNodeAsset(SkillNodeView nodeView, string requestedName)
    {
        UnityEngine.Object renameTarget = nodeView.Kind == SkillNodeKind.ActiveSkill ? _skill : nodeView.Asset;
        if (renameTarget == null)
        {
            return;
        }

        SkillGraphAssetUtility.RenameAsset(renameTarget, requestedName);
        nodeView.RefreshTitle();

        if (renameTarget == _skill)
        {
            _window.RefreshGraph();
        }
    }

    private void RemoveNodeReference(SkillNodeView nodeView)
    {
        if (_skill == null)
        {
            return;
        }

        if (nodeView.IsLoose)
        {
            if (nodeView.Asset is ScriptableObject looseAsset)
            {
                _looseAssets.Remove(looseAsset);
            }

            return;
        }

        switch (nodeView.Kind)
        {
            case SkillNodeKind.Trigger:
                Undo.RecordObject(_skill, "Remove Trigger");
                _skill.Trigger = null;
                SkillGraphAssetUtility.MarkDirty(_skill);
                break;
            case SkillNodeKind.Execution:
                if (nodeView.Key == "execution")
                {
                    Undo.RecordObject(_skill, "Remove Execution");
                    _skill.Execution = null;
                    SkillGraphAssetUtility.MarkDirty(_skill);
                }
                break;
            case SkillNodeKind.Target:
                Undo.RecordObject(_skill, "Remove Target");
                _skill.Target = null;
                SkillGraphAssetUtility.MarkDirty(_skill);
                break;
            case SkillNodeKind.ExecutionVfx:
                if (TryGetExecutionEffectVfxNodeInfo(nodeView, out string effectVfxOwnerNodeKey, out int effectVfxIndex)
                    && _nodes.TryGetValue(effectVfxOwnerNodeKey, out SkillNodeView effectVfxOwnerNode)
                    && effectVfxOwnerNode.Asset is ExecutionData effectVfxOwnerExecution
                    && effectVfxOwnerExecution.Effects != null
                    && effectVfxIndex >= 0
                    && effectVfxIndex < effectVfxOwnerExecution.Effects.Count)
                {
                    EffectBase effect = effectVfxOwnerExecution.Effects[effectVfxIndex];
                    if (effect != null)
                    {
                        Undo.RecordObject(effect, "Remove Effect VFX");
                        effect.VFX = null;
                        SkillGraphAssetUtility.MarkDirty(effect);
                    }
                }
                else if (TryGetExecutionVfxNodeInfo(nodeView, out string vfxOwnerNodeKey)
                    && _nodes.TryGetValue(vfxOwnerNodeKey, out SkillNodeView vfxOwnerNode)
                    && vfxOwnerNode.Asset is ExecutionData vfxOwnerExecution)
                {
                    Undo.RecordObject(vfxOwnerExecution, "Remove Execution VFX");
                    vfxOwnerExecution.VFX = null;
                    SkillGraphAssetUtility.MarkDirty(vfxOwnerExecution);
                }
                break;
            case SkillNodeKind.Effect:
                if (TryGetExecutionEffectNodeInfo(nodeView, out string effectOwnerNodeKey, out int executionEffectIndex)
                    && _nodes.TryGetValue(effectOwnerNodeKey, out SkillNodeView effectOwnerNode)
                    && effectOwnerNode.Asset is ExecutionData effectOwnerExecution
                    && effectOwnerExecution.Effects != null
                    && executionEffectIndex >= 0
                    && executionEffectIndex < effectOwnerExecution.Effects.Count)
                {
                    Undo.RecordObject(effectOwnerExecution, "Remove Effect");
                    effectOwnerExecution.Effects.RemoveAt(executionEffectIndex);
                    SkillGraphAssetUtility.MarkDirty(effectOwnerExecution);
                }
                else if (TryGetSummonItemDataEffectNodeInfo(nodeView, out string summonNodeKey, out int summonEffectIndex)
                    && _nodes.TryGetValue(summonNodeKey, out SkillNodeView summonNode)
                    && summonNode.Asset is SummonItemData summonData
                    && summonData.Effects != null
                    && summonEffectIndex >= 0
                    && summonEffectIndex < summonData.Effects.Count)
                {
                    Undo.RecordObject(summonData, "Remove Summon Effect");
                    summonData.Effects.RemoveAt(summonEffectIndex);
                    SkillGraphAssetUtility.MarkDirty(summonData);
                }
                break;
            case SkillNodeKind.ProjectileData:
            case SkillNodeKind.SummonItemData:
                if (TryGetExecutionEffectSummonNodeInfo(nodeView, out string summonOwnerNodeKey, out int executionSummonEffectIndex)
                    && _nodes.TryGetValue(summonOwnerNodeKey, out SkillNodeView summonOwnerNode)
                    && summonOwnerNode.Asset is ExecutionData summonOwnerExecution
                    && summonOwnerExecution.Effects != null
                    && executionSummonEffectIndex >= 0
                    && executionSummonEffectIndex < summonOwnerExecution.Effects.Count)
                {
                    EffectBase effect = summonOwnerExecution.Effects[executionSummonEffectIndex];
                    if (effect is SpawnEffect spawnEffect && spawnEffect.Item == nodeView.Asset)
                    {
                        Undo.RecordObject(spawnEffect, "Remove Spawn Item");
                        spawnEffect.Item = null;
                        SkillGraphAssetUtility.MarkDirty(spawnEffect);
                    }
                }
                break;
        }
    }
}

public sealed class SkillGraphRectangleSelector : MouseManipulator
{
    private readonly SkillGraphView _graphView;
    private VisualElement _selectionElement;
    private Vector2 _start;
    private bool _active;

    public SkillGraphRectangleSelector(SkillGraphView graphView)
    {
        _graphView = graphView;
        activators.Add(new ManipulatorActivationFilter
        {
            button = MouseButton.LeftMouse
        });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (_active || !CanStartManipulation(evt) || !IsBackgroundTarget(evt.target as VisualElement))
        {
            return;
        }

        _active = true;
        _start = GetGraphMousePosition(evt.mousePosition);

        if (!evt.shiftKey)
        {
            _graphView.ClearSelection();
        }

        _selectionElement = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };
        _selectionElement.style.position = Position.Absolute;
        _selectionElement.style.borderLeftWidth = 1f;
        _selectionElement.style.borderRightWidth = 1f;
        _selectionElement.style.borderTopWidth = 1f;
        _selectionElement.style.borderBottomWidth = 1f;
        _selectionElement.style.borderLeftColor = new Color(0.35f, 0.62f, 1f, 0.95f);
        _selectionElement.style.borderRightColor = new Color(0.35f, 0.62f, 1f, 0.95f);
        _selectionElement.style.borderTopColor = new Color(0.35f, 0.62f, 1f, 0.95f);
        _selectionElement.style.borderBottomColor = new Color(0.35f, 0.62f, 1f, 0.95f);
        _selectionElement.style.backgroundColor = new Color(0.35f, 0.62f, 1f, 0.12f);
        target.Add(_selectionElement);

        target.CaptureMouse();
        UpdateSelection(GetGraphMousePosition(evt.mousePosition), evt.shiftKey);
        evt.StopImmediatePropagation();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (!_active || !target.HasMouseCapture())
        {
            return;
        }

        UpdateSelection(GetGraphMousePosition(evt.mousePosition), evt.shiftKey);
        evt.StopImmediatePropagation();
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (!_active || !CanStopManipulation(evt))
        {
            return;
        }

        UpdateSelection(GetGraphMousePosition(evt.mousePosition), evt.shiftKey);
        StopSelection();
        evt.StopImmediatePropagation();
    }

    private void OnMouseCaptureOut(MouseCaptureOutEvent evt)
    {
        StopSelection();
    }

    private void UpdateSelection(Vector2 current, bool additive)
    {
        Rect selectionRect = GetSelectionRect(_start, current);
        UpdateSelectionElement(selectionRect);

        if (!additive)
        {
            _graphView.ClearSelection();
        }

        foreach (GraphElement element in _graphView.graphElements.ToList())
        {
            if (!IsSelectableElement(element))
            {
                continue;
            }

            Rect elementRect = GetGraphLocalRect(element);
            if (selectionRect.Overlaps(elementRect, true))
            {
                _graphView.AddToSelection(element);
            }
        }
    }

    private void UpdateSelectionElement(Rect rect)
    {
        if (_selectionElement == null)
        {
            return;
        }

        _selectionElement.style.left = rect.xMin;
        _selectionElement.style.top = rect.yMin;
        _selectionElement.style.width = rect.width;
        _selectionElement.style.height = rect.height;
    }

    private void StopSelection()
    {
        if (!_active)
        {
            return;
        }

        _active = false;

        if (target.HasMouseCapture())
        {
            target.ReleaseMouse();
        }

        _selectionElement?.RemoveFromHierarchy();
        _selectionElement = null;
    }

    private Vector2 GetGraphMousePosition(Vector2 mousePosition)
    {
        return _graphView.WorldToLocal(mousePosition);
    }

    private Rect GetGraphLocalRect(VisualElement element)
    {
        Vector2 min = _graphView.WorldToLocal(element.worldBound.min);
        Vector2 max = _graphView.WorldToLocal(element.worldBound.max);
        return Rect.MinMaxRect(
            Mathf.Min(min.x, max.x),
            Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x),
            Mathf.Max(min.y, max.y));
    }

    private static Rect GetSelectionRect(Vector2 a, Vector2 b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.x, b.x),
            Mathf.Min(a.y, b.y),
            Mathf.Max(a.x, b.x),
            Mathf.Max(a.y, b.y));
    }

    private static bool IsSelectableElement(GraphElement element)
    {
        return element != null
            && element.visible
            && (element.capabilities & Capabilities.Selectable) == Capabilities.Selectable;
    }

    private static bool IsBackgroundTarget(VisualElement element)
    {
        while (element != null)
        {
            if (element is Node || element is Edge || element is Port)
            {
                return false;
            }

            element = element.parent;
        }

        return true;
    }
}
#endif
