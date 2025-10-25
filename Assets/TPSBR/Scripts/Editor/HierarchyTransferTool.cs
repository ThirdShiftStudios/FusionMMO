#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using TPSBR;

public class HierarchyTransferTool : EditorWindow
{
    [SerializeField] private Transform currentRoot;
    [SerializeField] private Transform newRoot;

    [MenuItem("Tools/Hierarchy Transfer Tool")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<HierarchyTransferTool>("Hierarchy Transfer Tool");
        wnd.minSize = new Vector2(480, 280);
        wnd.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Transfer renderer configs, EquipmentVisual components, & ItemSlotTransform objects by matching hierarchy paths.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        currentRoot = (Transform)EditorGUILayout.ObjectField("Current Root", currentRoot, typeof(Transform), true);
        newRoot     = (Transform)EditorGUILayout.ObjectField("New Root", newRoot, typeof(Transform), true);

        using (new EditorGUI.DisabledScope(currentRoot == null || newRoot == null))
        {
            if (GUILayout.Button("Run Transfer", GUILayout.Height(36)))
            {
                RunTransfer();
            }
        }

        EditorGUILayout.HelpBox(
            "Notes:\n" +
            "• Matching is done by relative path from the given roots.\n" +
            "• SkinnedMeshRenderer bones/rootBone are remapped by the same path.\n" +
            "• If a renderer doesn’t exist on the New side, it will be added.\n" +
            "• EquipmentVisual components are moved to their equivalent GameObjects under New.\n" +
            "• ItemSlotTransform GameObjects are MOVED to the equivalent path under New.\n",
            MessageType.Info
        );
    }

    private void RunTransfer()
    {
        try
        {
            if (currentRoot == null || newRoot == null)
            {
                EditorUtility.DisplayDialog("Missing Roots", "Please assign both Current and New root transforms.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // Build path maps
            var currentMap = BuildPathMap(currentRoot);
            var newMap     = BuildPathMap(newRoot);

            // 1) Copy renderer configs
            int copiedMR = 0, copiedSMR = 0, addedMR = 0, addedSMR = 0, missingTargets = 0;
            int movedEquipmentVisuals = 0, skippedEquipmentVisuals = 0;

            foreach (var kvp in currentMap)
            {
                string relPath = kvp.Key;
                Transform currentT = kvp.Value;

                var equipmentVisuals = currentT.GetComponents<EquipmentVisual>();

                if (!newMap.TryGetValue(relPath, out Transform newT))
                {
                    if (equipmentVisuals != null && equipmentVisuals.Length > 0)
                    {
                        int validVisuals = 0;
                        foreach (var visual in equipmentVisuals)
                        {
                            if (visual != null)
                            {
                                validVisuals++;
                            }
                        }

                        skippedEquipmentVisuals += validVisuals;
                    }

                    missingTargets++;
                    continue; // No equivalent target in new hierarchy
                }

                // MeshRenderer + MeshFilter
                var currMR = currentT.GetComponent<MeshRenderer>();
                var currMF = currentT.GetComponent<MeshFilter>();
                if (currMR != null)
                {
                    var newMR = newT.GetComponent<MeshRenderer>();
                    if (newMR == null)
                    {
                        Undo.AddComponent<MeshRenderer>(newT.gameObject);
                        newMR = newT.GetComponent<MeshRenderer>();
                        addedMR++;
                    }
                    CopyMeshRenderer(currMR, newMR);

                    var newMF = newT.GetComponent<MeshFilter>();
                    if (currMF != null)
                    {
                        if (newMF == null)
                        {
                            Undo.AddComponent<MeshFilter>(newT.gameObject);
                            newMF = newT.GetComponent<MeshFilter>();
                        }
                        newMF.sharedMesh = currMF.sharedMesh;
                    }
                    copiedMR++;
                }

                // SkinnedMeshRenderer
                var currSMR = currentT.GetComponent<SkinnedMeshRenderer>();
                if (currSMR != null)
                {
                    var newSMR = newT.GetComponent<SkinnedMeshRenderer>();
                    if (newSMR == null)
                    {
                        Undo.AddComponent<SkinnedMeshRenderer>(newT.gameObject);
                        newSMR = newT.GetComponent<SkinnedMeshRenderer>();
                        addedSMR++;
                    }
                    CopySkinnedMeshRenderer(currSMR, newSMR, currentRoot, newRoot, newMap);
                    copiedSMR++;
                }

                // EquipmentVisual components
                if (equipmentVisuals != null && equipmentVisuals.Length > 0)
                {
                    foreach (var visual in equipmentVisuals)
                    {
                        if (visual == null)
                        {
                            skippedEquipmentVisuals++;
                            continue;
                        }

                        ComponentUtility.CopyComponent(visual);
                        if (!ComponentUtility.PasteComponentAsNew(newT.gameObject))
                        {
                            skippedEquipmentVisuals++;
                            continue;
                        }

                        Undo.DestroyObjectImmediate(visual);
                        movedEquipmentVisuals++;
                    }
                }
            }

            // 2) Move ItemSlotTransform GameObjects
            int movedItemSlots = 0, skippedItemSlots = 0;
            foreach (var kvp in currentMap)
            {
                Transform t = kvp.Value;
                if (HasComponentNamed(t.gameObject, "ItemSlotTransform"))
                {
                    var relPath = CalculatePath(currentRoot, t);

                    if (newMap.TryGetValue(relPath.Replace("/" + t.gameObject.name, ""), out Transform newEquivalentParent))
                    {
                        // Move the WHOLE GameObject under the equivalent transform in "New".
                        // We’ll keep local alignment (not world), so pass worldPositionStays = false.
                        Undo.SetTransformParent(t, newEquivalentParent, "Move ItemSlotTransform GameObject");
                        Vector3 localPos = new Vector3(t.localPosition.x, t.localPosition.y, t.localPosition.z);
                        Quaternion localRot = t.localRotation;
                        
                        t.SetParent(newEquivalentParent, false);
                        t.localPosition = localPos;
                        t.localRotation = localRot;
                        
                        movedItemSlots++;
                    }
                    else
                    {
                        skippedItemSlots++;
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "Hierarchy Transfer Complete",
                $"MeshRenderers copied: {copiedMR} (added: {addedMR})\n" +
                $"SkinnedMeshRenderers copied: {copiedSMR} (added: {addedSMR})\n" +
                $"EquipmentVisual components moved: {movedEquipmentVisuals} (skipped: {skippedEquipmentVisuals})\n" +
                $"Missing equivalent targets in New: {missingTargets}\n" +
                $"ItemSlotTransform GameObjects moved: {movedItemSlots} (skipped: {skippedItemSlots})",
                "OK"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Hierarchy Transfer Tool Error: {ex}");
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    // --- Helpers ---

    private static Dictionary<string, Transform> BuildPathMap(Transform root)
    {
        var map = new Dictionary<string, Transform>(StringComparer.Ordinal);
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string path = CalculatePath(root, t);
            map[path] = t;
        }
        return map;
    }

    private static string CalculatePath(Transform root, Transform t)
    {
        // Prefer AnimationUtility path when available; otherwise build manually
        #if UNITY_EDITOR
        return AnimationUtility.CalculateTransformPath(t, root);
        #else
        // Fallback (shouldn't happen in editor code)
        var stack = new System.Collections.Generic.Stack<string>();
        var cur = t;
        while (cur != null && cur != root)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack.ToArray());
        #endif
    }

    private static bool HasComponentNamed(GameObject go, string typeName)
    {
        // Avoid hard dependency on the assembly that defines ItemSlotTransform.
        // Look for any component whose type.Name matches.
        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null) continue; // missing script
            if (string.Equals(c.GetType().Name, typeName, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static void CopyMeshRenderer(MeshRenderer src, MeshRenderer dst)
    {
        Undo.RecordObject(dst, "Copy MeshRenderer");

        // Common-ish settings
        dst.enabled = src.enabled;
        dst.lightProbeUsage = src.lightProbeUsage;
        dst.reflectionProbeUsage = src.reflectionProbeUsage;
        dst.probeAnchor = src.probeAnchor;
        dst.additionalVertexStreams = src.additionalVertexStreams;
        dst.receiveShadows = src.receiveShadows;
        dst.shadowCastingMode = src.shadowCastingMode;
#if UNITY_2021_2_OR_NEWER
        dst.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;
#endif
        dst.motionVectorGenerationMode = src.motionVectorGenerationMode;
        dst.renderingLayerMask = src.renderingLayerMask;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;

        // Materials: use sharedMaterials to avoid instantiating
        dst.sharedMaterials = src.sharedMaterials;
    }

    private static void CopySkinnedMeshRenderer(
        SkinnedMeshRenderer src,
        SkinnedMeshRenderer dst,
        Transform currentRoot,
        Transform newRoot,
        Dictionary<string, Transform> newMap)
    {
        Undo.RecordObject(dst, "Copy SkinnedMeshRenderer");

        // Basic flags & render settings
        dst.enabled = src.enabled;
        dst.updateWhenOffscreen = src.updateWhenOffscreen;
#if UNITY_2020_2_OR_NEWER
        dst.skinnedMotionVectors = src.skinnedMotionVectors;
#endif
        dst.receiveShadows = src.receiveShadows;
        dst.shadowCastingMode = src.shadowCastingMode;
        dst.lightProbeUsage = src.lightProbeUsage;
        dst.reflectionProbeUsage = src.reflectionProbeUsage;
        dst.probeAnchor = src.probeAnchor;
        dst.quality = src.quality;
        dst.renderingLayerMask = src.renderingLayerMask;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;

        // Mesh & materials
        dst.sharedMesh = src.sharedMesh;
        dst.sharedMaterials = src.sharedMaterials;

        // Root bone remap
        dst.rootBone = RemapBoneByPath(src.rootBone, currentRoot, newMap);

        // Bones remap
        Transform[] srcBones = src.bones ?? Array.Empty<Transform>();
        var remapped = new Transform[srcBones.Length];
        for (int i = 0; i < srcBones.Length; i++)
        {
            remapped[i] = RemapBoneByPath(srcBones[i], currentRoot, newMap);
        }
        dst.bones = remapped;

        // Blendshape weights
        if (src.sharedMesh != null && dst.sharedMesh == src.sharedMesh)
        {
            int count = src.sharedMesh.blendShapeCount;
            // Ensure same blendshape count; if meshes differ, we skip weights
            if (count > 0)
            {
                // SkinnedMeshRenderer stores weights per index
                for (int i = 0; i < count; i++)
                {
                    float w = src.GetBlendShapeWeight(i);
                    dst.SetBlendShapeWeight(i, w);
                }
            }
        }

        // Bounds (optional but often helpful when offscreen updates differ)
        dst.localBounds = src.localBounds;
    }

    private static Transform RemapBoneByPath(Transform bone, Transform currentRoot, Dictionary<string, Transform> newMap)
    {
        if (bone == null) return null;
        string bonePath = CalculatePath(currentRoot, bone);
        newMap.TryGetValue(bonePath, out Transform mapped);
        return mapped;
    }
}
#endif
