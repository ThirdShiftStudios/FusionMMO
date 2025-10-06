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

        private void OnEnable()
        {
            _treeState ??= new TreeViewState();
            _treeView ??= new DashboardTreeView(_treeState);
            _treeView.onSelectionChanged += OnTreeSelectionChanged;
            _searchField ??= new SearchField();
        }

        private void OnDisable()
        {
            _treeView.onSelectionChanged -= OnTreeSelectionChanged;
            DestroyImmediate(_activeInspector);
            _activeInspector = null;
            _activeSelection = null;
        }

        private void OnGUI()
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
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
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
                _activeInspector.OnInspectorGUI();
            }
        }

        private void OnTreeSelectionChanged(UnityEngine.Object obj)
        {
            if (_activeSelection == obj)
                return;

            _activeSelection = obj;
            DestroyImmediate(_activeInspector);
            _activeInspector = null;

            Repaint();
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
            .Select(p => AssetDatabase.LoadAssetAtPath(p, type) as DataDefinition)
            .Where(a => a != null)
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var asset in assets)
        {
            var icon = asset.Icon != null ? asset.Icon : AssetPreview.GetMiniThumbnail(asset);
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
        for (int i = 0; i < args.GetNumVisibleColumns(); i++)
        {
            var cellRect = args.GetCellRect(i);
            CenterRectUsingSingleLineHeight(ref cellRect);

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

            EditorGUI.LabelField(cellRect, item.displayName);
        }
    }


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
