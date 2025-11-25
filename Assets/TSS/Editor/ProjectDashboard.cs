// Assets/TSS/Editor/ProjectDashboard.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using TSS.Data;

namespace TSS.Tools
{
    public sealed class ProjectDashboard : EditorWindow
    {
        private const float LeftPaneMin = 220f;
        private const float LeftPaneMax = 480f;
        private const string EditorPrefsKey = "TSS_ProjectDashboard_Tasks";

        private enum DashboardView
        {
            Data,
            Tasks
        }

        private enum TaskStatus
        {
            NotStarted,
            InProgress,
            Blocked,
            Done
        }

        private enum TaskPriority
        {
            Low,
            Medium,
            High,
            Critical
        }

        [Serializable]
        private sealed class DashboardTask
        {
            public string name;
            public TaskStatus status;
            public TaskPriority priority;
            public string description;
        }

        [MenuItem("TSS/Tools/Project Dashboard")]
        public static void Open()
        {
            var wnd = GetWindow<ProjectDashboard>("Project Dashboard");
            wnd.minSize = new Vector2(720, 420);
            wnd.Show();
        }

        private TreeViewState _treeState;
        private SearchField _searchField;
        private DashboardTreeView _treeView;

        private Editor _activeInspector;
        private UnityEngine.Object _activeSelection;
        private Vector2 _inspectorScroll;

        private DashboardView _activeView = DashboardView.Data;

        private List<DashboardTask> _tasks;
        private int _selectedTaskIndex;
        private Vector2 _taskListScroll;
        private Vector2 _taskDetailScroll;

        private void OnEnable()
        {
            EnsureTreeInitialized();
            EnsureTasksInitialized();
        }

        private void OnDisable()
        {
            if (_treeView != null)
            {
                _treeView.onSelectionChanged -= OnTreeSelectionChanged;
            }
            DestroyImmediate(_activeInspector);
            _activeInspector = null;
            _activeSelection = null;

            SaveTasks();
        }

        private void OnGUI()
        {
            EnsureTreeInitialized();
            EnsureTasksInitialized();

            DrawViewTabs();
            GUILayout.Space(4f);

            if (_activeView == DashboardView.Data)
            {
                DrawDataView();
            }
            else
            {
                DrawTasksView();
            }
        }

        private void DrawViewTabs()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Toggle(_activeView == DashboardView.Data, "Data", EditorStyles.toolbarButton))
                {
                    _activeView = DashboardView.Data;
                }

                if (GUILayout.Toggle(_activeView == DashboardView.Tasks, "Tasks", EditorStyles.toolbarButton))
                {
                    _activeView = DashboardView.Tasks;
                }
            }
        }

        private void DrawDataView()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * 0.35f, LeftPaneMin, LeftPaneMax))))
                {
                    DrawLeftPane();
                }

                GUILayout.Box(GUIContent.none, GUILayout.ExpandHeight(true), GUILayout.Width(1));

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawRightPane();
                }
            }
        }

        private void DrawLeftPane()
        {
            if (_treeView == null)
            {
                EditorGUILayout.HelpBox("Failed to initialize dashboard tree.", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    _treeView.Reload();
                }

                if (GUILayout.Button("Recalculate IDs", EditorStyles.toolbarButton, GUILayout.Width(120)))
                {
                    DefinitionIdProvider.instance.RecalculateAllDefinitionIds();
                    _treeView.Reload();
                }

                GUILayout.FlexibleSpace();
                _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
            }

            var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _treeView.OnGUI(rect);
        }

        private void DrawRightPane()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_activeSelection ? _activeSelection.name : "No Selection", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }

            if (_activeSelection == null)
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("Select a node to view details.", EditorStyles.centeredGreyMiniLabel);
                }
                GUILayout.FlexibleSpace();
                return;
            }

            if (_activeInspector == null)
            {
                _activeInspector = Editor.CreateEditor(_activeSelection);
            }

            if (_activeInspector != null)
            {
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(_inspectorScroll))
                {
                    _inspectorScroll = scrollScope.scrollPosition;
                    _activeInspector.OnInspectorGUI();
                }
            }
        }

        private void OnTreeSelectionChanged(UnityEngine.Object obj)
        {
            if (_activeSelection == obj)
                return;

            _activeSelection = obj;
            DestroyImmediate(_activeInspector);
            _activeInspector = null;
            _inspectorScroll = Vector2.zero;

            Repaint();
        }

        private void EnsureTreeInitialized()
        {
            _treeState ??= new TreeViewState();
            _searchField ??= new SearchField();

            if (_treeView == null)
            {
                _treeView = new DashboardTreeView(_treeState);
                _treeView.onSelectionChanged += OnTreeSelectionChanged;
            }
        }

        private void EnsureTasksInitialized()
        {
            if (_tasks != null)
                return;

            _tasks = LoadTasks();

            if (_tasks.Count == 0)
            {
                _tasks = new List<DashboardTask>
                {
                    new DashboardTask
                    {
                        name = "Review data definitions",
                        status = TaskStatus.InProgress,
                        priority = TaskPriority.High,
                        description = "Validate that all data definition assets are configured and have unique IDs."
                    },
                    new DashboardTask
                    {
                        name = "Add new quest entries",
                        status = TaskStatus.NotStarted,
                        priority = TaskPriority.Medium,
                        description = "Create quest data entries for the upcoming content drop."
                    },
                    new DashboardTask
                    {
                        name = "Cleanup unused assets",
                        status = TaskStatus.Blocked,
                        priority = TaskPriority.Low,
                        description = "Remove obsolete prototypes after confirming they are no longer referenced."
                    }
                };
            }

            _selectedTaskIndex = _tasks.Count > 0
                ? Mathf.Clamp(_selectedTaskIndex, 0, _tasks.Count - 1)
                : -1;
        }

        private void DrawTasksView()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * 0.35f, LeftPaneMin, LeftPaneMax))))
                {
                    DrawTaskList();
                }

                GUILayout.Box(GUIContent.none, GUILayout.ExpandHeight(true), GUILayout.Width(1));

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawTaskDetails();
                }
            }
        }

        private void DrawTaskList()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Tasks", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("New Task", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    CreateNewTask();
                }
            }

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_taskListScroll))
            {
                _taskListScroll = scrollScope.scrollPosition;

                for (int i = 0; i < _tasks.Count; i++)
                {
                    var task = _tasks[i];
                    bool isSelected = i == _selectedTaskIndex;

                    var style = new GUIStyle(EditorStyles.helpBox)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(8, 8, 6, 6)
                    };

                    if (isSelected)
                    {
                        style.normal.background = EditorGUIUtility.isProSkin
                            ? Texture2D.grayTexture
                            : Texture2D.whiteTexture;
                        style.normal.textColor = EditorStyles.boldLabel.normal.textColor;
                    }

                    var rowRect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true));

                    if (GUI.Button(rowRect, GUIContent.none, style))
                    {
                        _selectedTaskIndex = i;
                        GUI.FocusControl(null);
                    }

                    var content = new GUIContent(
                        $"{task.name}\nStatus: {task.status}   Priority: {task.priority}");
                    GUI.Label(rowRect, content, style);

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(1); // maintain layout spacing after custom rect
                    }

                    GUILayout.Space(6f);
                }
            }
        }

        private void DrawTaskDetails()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_tasks.Count > 0 && _selectedTaskIndex >= 0 && _selectedTaskIndex < _tasks.Count
                    ? _tasks[_selectedTaskIndex].name
                    : "No Task Selected", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }

            if (_tasks.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("No tasks available.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                return;
            }

            _selectedTaskIndex = Mathf.Clamp(_selectedTaskIndex, 0, _tasks.Count - 1);
            var task = _tasks[_selectedTaskIndex];

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_taskDetailScroll))
            {
                _taskDetailScroll = scrollScope.scrollPosition;

                EditorGUI.BeginChangeCheck();

                task.name = EditorGUILayout.TextField("Name", task.name);
                task.status = (TaskStatus)EditorGUILayout.EnumPopup("Status", task.status);
                task.priority = (TaskPriority)EditorGUILayout.EnumPopup("Priority", task.priority);

                GUILayout.Label("Description");
                task.description = EditorGUILayout.TextArea(task.description, GUILayout.MinHeight(80));

                if (EditorGUI.EndChangeCheck())
                {
                    _tasks[_selectedTaskIndex] = task;
                    SaveTasks();
                    Repaint();
                }
            }
        }

        private void CreateNewTask()
        {
            var newTask = new DashboardTask
            {
                name = $"New Task {_tasks.Count + 1}",
                status = TaskStatus.NotStarted,
                priority = TaskPriority.Medium,
                description = string.Empty
            };

            _tasks.Add(newTask);
            _selectedTaskIndex = _tasks.Count - 1;
            SaveTasks();
            Repaint();
        }

        private List<DashboardTask> LoadTasks()
        {
            if (!EditorPrefs.HasKey(EditorPrefsKey))
                return new List<DashboardTask>();

            try
            {
                var raw = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
                var wrapper = JsonUtility.FromJson<TaskCollection>(raw);
                if (wrapper?.tasks != null)
                {
                    return wrapper.tasks;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ProjectDashboard: Failed to load tasks, using defaults. {e.Message}");
            }

            return new List<DashboardTask>();
        }

        private void SaveTasks()
        {
            if (_tasks == null)
                return;

            var wrapper = new TaskCollection { tasks = _tasks };
            var json = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(EditorPrefsKey, json);
        }

        [Serializable]
        private sealed class TaskCollection
        {
            public List<DashboardTask> tasks;
        }

        // ---------------- TreeView ----------------

        private sealed class DashboardTreeView : TreeView
{
    private const int RootId = 0;
    private const int DataDefsId = 1;
    private const int ProjectSettingsId = 2;
    private const int GameSettingsId = 3;

    private readonly Texture2D _folderIcon = EditorGUIUtility.FindTexture("Folder Icon");

    public event Action<UnityEngine.Object> onSelectionChanged;

    // Unique ID generator for all items created here (never use GetInstanceID or GetHashCode).
    private int _idCounter = 1000;
    private int NewId() => ++_idCounter;

    public DashboardTreeView(TreeViewState state)
        : base(state, new MultiColumnHeader(MakeHeader()))
    {
        showAlternatingRowBackgrounds = true;
        showBorder = false;
        Reload();
    }

    private static MultiColumnHeaderState MakeHeader()
    {
        var cols = new[]
        {
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Items"),
                headerTextAlignment = TextAlignment.Left,
                canSort = false,
                width = 250,
                minWidth = 150,
                autoResize = true
            }
        };
        return new MultiColumnHeaderState(cols);
    }

    protected override TreeViewItem BuildRoot()
    {
        _idCounter = 1000; // reset per rebuild

        var root = new DashboardItem(RootId, -1, "Root", null);

        var dataDefs = new DashboardItem(DataDefsId, 0, "Data Definitions", _folderIcon);
        var projectSettings = new DashboardItem(ProjectSettingsId, 0, "Project Settings", _folderIcon);
        var gameSettings = new DashboardItem(GameSettingsId, 0, "Game Settings", _folderIcon);

        root.AddChild(dataDefs);
        root.AddChild(projectSettings);
        root.AddChild(gameSettings);

        PopulateDataDefinitions(dataDefs);

        SetupDepthsFromParentsAndChildren(root);

        // Ensure the user can see content immediately.
        SetExpanded(DataDefsId, true);

        return root;
    }

    private static IEnumerable<Type> GetAllDefinitionTypes()
    {
        return TypeCache.GetTypesDerivedFrom<DataDefinition>()
            .Where(t => !t.IsGenericType && typeof(ScriptableObject).IsAssignableFrom(t));
    }

    private static IEnumerable<Type> OrderTypes(IEnumerable<Type> types)
    {
        return types.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static Type GetParentDefinitionType(Type type)
    {
        var baseType = type.BaseType;

        while (baseType != null)
        {
            if (baseType == typeof(DataDefinition))
                return null;

            if (typeof(DataDefinition).IsAssignableFrom(baseType) && typeof(ScriptableObject).IsAssignableFrom(baseType))
                return baseType;

            baseType = baseType.BaseType;
        }

        return null;
    }

    private void PopulateDataDefinitions(DashboardItem parent)
    {
        var allTypes = GetAllDefinitionTypes().ToList();

        var childLookup = new Dictionary<Type, List<Type>>();
        var rootTypes = new List<Type>();

        foreach (var type in allTypes)
        {
            var parentType = GetParentDefinitionType(type);
            if (parentType == null)
            {
                rootTypes.Add(type);
            }
            else
            {
                if (!childLookup.TryGetValue(parentType, out var list))
                {
                    list = new List<Type>();
                    childLookup[parentType] = list;
                }

                list.Add(type);
            }
        }

        foreach (var type in childLookup.Keys.ToList())
        {
            childLookup[type] = OrderTypes(childLookup[type]).ToList();
        }

        foreach (var rootType in OrderTypes(rootTypes))
        {
            var node = CreateTypeNode(rootType, parent.depth + 1);
            AddAssetsForType(node, rootType);
            parent.AddChild(node);
            PopulateChildTypes(node, rootType, childLookup);
            SetExpanded(node.id, true);
        }
    }

    private void PopulateChildTypes(DashboardItem parentNode, Type parentType, Dictionary<Type, List<Type>> childLookup)
    {
        if (!childLookup.TryGetValue(parentType, out var children))
            return;

        foreach (var childType in children)
        {
            var childNode = CreateTypeNode(childType, parentNode.depth + 1);
            AddAssetsForType(childNode, childType);
            parentNode.AddChild(childNode);
            PopulateChildTypes(childNode, childType, childLookup);
            SetExpanded(childNode.id, true);
        }
    }

    private DashboardItem CreateTypeNode(Type type, int depth)
    {
        return new DashboardItem(NewId(), depth, type.Name, EditorGUIUtility.FindTexture("d_ScriptableObject Icon"))
        {
            payload = type
        };
    }

    private void AddAssetsForType(DashboardItem parentNode, Type type)
    {
        if (type.IsAbstract)
            return;

        var guids = AssetDatabase.FindAssets($"t:{type.Name}");
        var assets = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<DataDefinition>(p))
            .Where(a => a != null && a.GetType() == type)
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var asset in assets)
        {
            Texture icon;

            if (asset.Icon != null)
            {
                icon = AssetPreview.GetAssetPreview(asset.Icon);
                if (icon == null)
                {
                    icon = asset.Icon.texture;
                }
            }
            else
            {
                icon = AssetPreview.GetMiniThumbnail(asset);
            }

            var label = string.IsNullOrEmpty(asset.Name) ? asset.name : asset.Name;

            var item = new DashboardItem(NewId(), parentNode.depth + 1, label, icon)
            {
                payload = asset
            };

            parentNode.AddChild(item);
        }
    }

    protected override void SingleClickedItem(int id)
    {
        var item = FindItem(id, rootItem) as DashboardItem;
        if (item?.payload is UnityEngine.Object obj)
        {
            onSelectionChanged?.Invoke(obj);
            EditorGUIUtility.PingObject(obj);
        }
        else
        {
            onSelectionChanged?.Invoke(null);
        }
    }

    protected override void DoubleClickedItem(int id)
    {
        var item = FindItem(id, rootItem) as DashboardItem;
        if (item?.payload is UnityEngine.Object obj)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }

    protected override void ContextClickedItem(int id)
    {
        var item = FindItem(id, rootItem) as DashboardItem;
        var menu = new GenericMenu();

        if (item?.payload is Type type && typeof(DataDefinition).IsAssignableFrom(type))
        {
            menu.AddItem(new GUIContent("Create New"), false, () =>
            {
                var asset = ScriptableObject.CreateInstance(type);
                var path = EditorUtility.SaveFilePanelInProject($"Create {type.Name}", type.Name, "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                    Reload();
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            });
        }

        if (item?.payload is UnityEngine.Object obj)
        {
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Select In Project"), false, () =>
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            });
        }

        if (menu.GetItemCount() > 0)
            menu.ShowAsContext();
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        var item = (DashboardItem)args.item;
        var definition = item.payload as DataDefinition;
        var highlightAsMissingId = definition != null && definition.ID == 0;

        for (int i = 0; i < args.GetNumVisibleColumns(); i++)
        {
            var cellRect = args.GetCellRect(i);
            CenterRectUsingSingleLineHeight(ref cellRect);

            if (i == 0)
            {
                DrawHierarchyLines(args.rowRect, item);
            }

            // indent based on depth
            float indent = GetContentIndent(item);
            cellRect.xMin += indent;

            if (item.icon != null)
            {
                const float iconSize = 16f;
                var iconRect = new Rect(cellRect.x, cellRect.y, iconSize, iconSize);
                GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit, true);
                cellRect.xMin += iconSize + 4f;
            }

            var previousColor = GUI.contentColor;
            if (highlightAsMissingId)
            {
                GUI.contentColor = Color.red;
            }

            EditorGUI.LabelField(cellRect, item.displayName);

            GUI.contentColor = previousColor;
        }
    }


    private void DrawHierarchyLines(Rect rowRect, DashboardItem item)
    {
        if (item.depth <= 0)
            return;

        var lineColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.25f)
            : new Color(0f, 0f, 0f, 0.35f);

        float midY = rowRect.center.y;
        float parentX = 0f;
        var parent = item.parent as DashboardItem;

        if (parent != null)
        {
            parentX = CalculateLineX(parent, rowRect.x);
            float bottom = IsLastChild(item) ? midY : rowRect.yMax;
            DrawVerticalLine(parentX, rowRect.y, bottom, lineColor);
        }

        var ancestor = parent?.parent as DashboardItem;
        while (ancestor != null && ancestor.depth >= 0)
        {
            if (!IsLastChild(ancestor))
            {
                float ancestorX = CalculateLineX(ancestor, rowRect.x);
                DrawVerticalLine(ancestorX, rowRect.y, rowRect.yMax, lineColor);
            }

            ancestor = ancestor.parent as DashboardItem;
        }

        float endX = rowRect.x + GetContentIndent(item) - HorizontalPadding;
        float startX = parent != null ? parentX : CalculateLineX(item, rowRect.x);

        if (endX > startX)
        {
            DrawHorizontalLine(startX, endX, midY, lineColor);
        }
    }


    private float CalculateLineX(TreeViewItem item, float rowOriginX)
    {
        float indent = GetContentIndent(item);

        if (indent <= 0f)
            return rowOriginX + DefaultIndentWidth * 0.5f;

        float parentIndent = (item.parent != null && item.parent.depth >= 0)
            ? GetContentIndent(item.parent)
            : Mathf.Max(0f, indent - DefaultIndentWidth);

        float delta = Mathf.Max(DefaultIndentWidth, indent - parentIndent);
        return rowOriginX + indent - (delta * 0.5f);
    }


    private static bool IsLastChild(DashboardItem item)
    {
        if (item?.parent == null)
            return true;

        var siblings = item.parent.children;
        if (siblings == null || siblings.Count == 0)
            return true;

        return ReferenceEquals(item, siblings[siblings.Count - 1]);
    }


    private static void DrawVerticalLine(float x, float yMin, float yMax, Color color)
    {
        if (yMax <= yMin)
            return;

        var rect = new Rect(Mathf.Round(x - (LineThickness * 0.5f)), Mathf.Round(yMin), LineThickness, Mathf.Round(yMax - yMin));
        EditorGUI.DrawRect(rect, color);
    }


    private static void DrawHorizontalLine(float xMin, float xMax, float y, Color color)
    {
        if (xMax <= xMin)
            return;

        var rect = new Rect(Mathf.Round(xMin), Mathf.Round(y - (LineThickness * 0.5f)), Mathf.Round(xMax - xMin), LineThickness);
        EditorGUI.DrawRect(rect, color);
    }


    private const float DefaultIndentWidth = 14f;
    private const float HorizontalPadding = 6f;
    private const float LineThickness = 1f;


    // change the DashboardItem field type
    private sealed class DashboardItem : TreeViewItem
    {
        public Texture icon;       // was Texture2D
        public object payload;

        public DashboardItem(int id, int depth, string displayName, Texture iconTex) : base(id, depth, displayName)
        {
            icon = iconTex;
        }
    }

}

    }
}
#endif
