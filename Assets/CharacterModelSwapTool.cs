using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility component that remaps references from an existing ("Current")
/// model hierarchy to a new one. Attach this component to the root of the
/// character GameObject, assign the two hierarchies, and run the swap from
/// the inspector.
/// </summary>
public class CharacterModelSwapTool : MonoBehaviour
{
    [SerializeField]
    private Transform currentRoot;

    [SerializeField]
    private Transform newRoot;

#if UNITY_EDITOR
    /// <summary>
    /// Executes the swap operation. This is also exposed through the custom
    /// inspector and a context menu to make it easy to trigger.
    /// </summary>
    [ContextMenu("Apply Character Model Swap")]
    public void ApplySwap()
    {
        if (currentRoot == null || newRoot == null)
        {
            Debug.LogError("CharacterModelSwapTool requires both Current and New transforms to be assigned before running.", this);
            return;
        }

        if (currentRoot == newRoot)
        {
            Debug.LogWarning("Current and New transforms are the same object. Swap aborted.", this);
            return;
        }

        try
        {
            ExecuteSwap();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Character model swap failed: {ex.Message}\n{ex.StackTrace}", this);
        }
    }

    private void ExecuteSwap()
    {
        var currentMap = BuildPathMap(currentRoot);
        var newMap = BuildPathMap(newRoot);

        UnityEditor.Undo.IncrementCurrentGroup();
        UnityEditor.Undo.SetCurrentGroupName("Character Model Swap");
        var undoGroup = UnityEditor.Undo.GetCurrentGroup();

        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Character Model Swap");
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(newRoot.gameObject, "Character Model Swap");
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(currentRoot.gameObject, "Character Model Swap");

        var movedCount = MoveMissingHierarchy(currentMap, newMap);

        // Rebuild the new hierarchy map to include any moved objects.
        newMap = BuildPathMap(newRoot);

        var updatedReferences = SwapComponentReferences(newMap);

        UnityEditor.Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.EditorUtility.SetDirty(gameObject);

        Debug.Log($"Character model swap completed. Moved {movedCount} transform(s) and updated {updatedReferences} reference(s).", this);
    }

    private int MoveMissingHierarchy(Dictionary<string, Transform> currentMap, Dictionary<string, Transform> newMap)
    {
        // We sort by depth so that parents are processed before their children.
        var entries = new List<KeyValuePair<string, Transform>>(currentMap);
        entries.Sort((a, b) => GetDepth(a.Key).CompareTo(GetDepth(b.Key)));

        var movedCount = 0;

        foreach (var kvp in entries)
        {
            var path = kvp.Key;
            var transform = kvp.Value;

            if (transform == currentRoot)
            {
                continue; // skip the root itself
            }

            if (newMap.ContainsKey(path))
            {
                continue; // already exists in the new hierarchy
            }

            var parentPath = GetParentPath(path);
            Transform targetParent;

            if (string.IsNullOrEmpty(parentPath))
            {
                targetParent = newRoot;
            }
            else if (!newMap.TryGetValue(parentPath, out targetParent))
            {
                // Parent may also be missing; it will be moved when its own turn comes.
                continue;
            }

            UnityEditor.Undo.SetTransformParent(transform, targetParent, "Character Model Swap - Move Missing Objects");
            movedCount++;
        }

        return movedCount;
    }

    private int SwapComponentReferences(Dictionary<string, Transform> newMap)
    {
        var components = GetComponents<Component>();
        var updatedCount = 0;

        foreach (var component in components)
        {
            if (component == null || component == this)
            {
                continue;
            }

            var so = new UnityEditor.SerializedObject(component);
            var iterator = so.GetIterator();
            var changed = false;

            UnityEditor.Undo.RecordObject(component, "Character Model Swap - Update References");

            // Iterate over all serialized properties to find references.
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyType != UnityEditor.SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.name == "m_Script")
                {
                    continue;
                }

                var referenced = iterator.objectReferenceValue;
                if (referenced == null)
                {
                    continue;
                }

                if (TryRemapReference(referenced, newMap, out var newReference))
                {
                    iterator.objectReferenceValue = newReference;
                    changed = true;
                    updatedCount++;
                }
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(component);
            }
        }

        return updatedCount;
    }

    private bool TryRemapReference(UnityEngine.Object reference, Dictionary<string, Transform> newMap, out UnityEngine.Object remapped)
    {
        remapped = null;

        Transform referenceTransform = ExtractTransform(reference);
        if (referenceTransform == null)
        {
            return false;
        }

        if (!IsDescendantOrSelf(referenceTransform, currentRoot))
        {
            return false;
        }

        var relativePath = GetRelativePath(referenceTransform, currentRoot);
        if (!newMap.TryGetValue(relativePath, out var newTransform))
        {
            Debug.LogWarning($"No matching transform found in the new hierarchy for path '{relativePath}'. Reference will be left unchanged.", referenceTransform);
            return false;
        }

        remapped = RemapObject(reference, referenceTransform, newTransform);
        return remapped != null;
    }

    private static UnityEngine.Object RemapObject(UnityEngine.Object originalReference, Transform oldTransform, Transform newTransform)
    {
        switch (originalReference)
        {
            case GameObject _:
                return newTransform.gameObject;
            case Transform _:
                return newTransform;
            case Component oldComponent:
                var type = oldComponent.GetType();
                var oldComponents = oldTransform.GetComponents(type);
                var newComponents = newTransform.GetComponents(type);
                var index = Array.IndexOf(oldComponents, oldComponent);

                if (index >= 0 && index < newComponents.Length)
                {
                    return newComponents[index];
                }

                Debug.LogWarning($"Could not find component of type {type.Name} at the same index on the new transform '{newTransform.name}'.", newTransform);
                return null;
            default:
                return null;
        }
    }

    private static Transform ExtractTransform(UnityEngine.Object obj)
    {
        switch (obj)
        {
            case Transform transform:
                return transform;
            case GameObject go:
                return go.transform;
            case Component component:
                return component.transform;
            default:
                return null;
        }
    }

    private static Dictionary<string, Transform> BuildPathMap(Transform root)
    {
        var map = new Dictionary<string, Transform>();
        BuildPathRecursive(root, string.Empty, map);
        return map;
    }

    private static void BuildPathRecursive(Transform current, string path, Dictionary<string, Transform> map)
    {
        map[path] = current;

        for (var i = 0; i < current.childCount; i++)
        {
            var child = current.GetChild(i);
            var childPath = string.IsNullOrEmpty(path) ? child.name : $"{path}/{child.name}";
            BuildPathRecursive(child, childPath, map);
        }
    }

    private static bool IsDescendantOrSelf(Transform candidate, Transform root)
    {
        while (candidate != null)
        {
            if (candidate == root)
            {
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }

    private static string GetRelativePath(Transform target, Transform root)
    {
        if (target == root)
        {
            return string.Empty;
        }

        var stack = new Stack<string>();
        var current = target;

        while (current != null && current != root)
        {
            stack.Push(current.name);
            current = current.parent;
        }

        if (current != root)
        {
            throw new InvalidOperationException("Target is not a child of the provided root.");
        }

        return string.Join("/", stack.ToArray());
    }

    private static string GetParentPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var lastSlash = path.LastIndexOf('/');
        return lastSlash >= 0 ? path.Substring(0, lastSlash) : string.Empty;
    }

    private static int GetDepth(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        var depth = 1;
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] == '/')
            {
                depth++;
            }
        }

        return depth;
    }
#endif
}
