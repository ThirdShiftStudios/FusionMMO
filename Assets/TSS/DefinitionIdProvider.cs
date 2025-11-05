// Assets/TSS/Editor/DefinitionIdProvider.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TSS.Data
{
    [FilePath("ProjectSettings/TSS_DefinitionIdProvider.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class DefinitionIdProvider : ScriptableSingleton<DefinitionIdProvider>
    {
        [SerializeField]
        private int lastAssignedId = -1;

        public int LastAssignedId => lastAssignedId;

        public int GetNextIdAndPersist()
        {
            var maxExistingId = Math.Max(lastAssignedId, GetCurrentMaxDefinitionId());
            var nextId = maxExistingId + 1;

            if (nextId != lastAssignedId)
            {
                lastAssignedId = nextId;
                Persist();
            }

            return lastAssignedId;
        }

        public void ResetLastAssignedId()
        {
            lastAssignedId = -1;
            Persist();
        }

        public void RecalculateAllDefinitionIds()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(DataDefinition)}");
            var definitionsWithZeroId = new List<(DataDefinition definition, string path)>();

            var maxExistingId = -1;

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                var definition = AssetDatabase.LoadAssetAtPath<DataDefinition>(assetPath);
                if (definition == null)
                    continue;

                maxExistingId = Mathf.Max(maxExistingId, definition.ID);

                if (definition.ID == 0)
                {
                    definitionsWithZeroId.Add((definition, assetPath));
                }
            }

            var orderedDefinitions = definitionsWithZeroId
                .OrderBy(d => d.path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (orderedDefinitions.Count == 0)
            {
                UpdateLastAssignedId(maxExistingId);
                return;
            }

            var nextId = Mathf.Max(lastAssignedId, maxExistingId) + 1;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var (definition, _) in orderedDefinitions)
                {
                    AssignId(definition, nextId);
                    nextId++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            UpdateLastAssignedId(nextId - 1);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void UpdateLastAssignedId(int candidateId)
        {
            var newLastAssignedId = Mathf.Max(candidateId, lastAssignedId);

            if (newLastAssignedId != lastAssignedId)
            {
                lastAssignedId = newLastAssignedId;
                Persist();
            }
        }

        private static void AssignId(DataDefinition definition, int newId)
        {
            var serializedObject = new SerializedObject(definition);
            serializedObject.Update();
            var idProperty = serializedObject.FindProperty("id");

            if (idProperty == null)
                return;

            idProperty.intValue = newId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private int GetCurrentMaxDefinitionId()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(DataDefinition)}");
            var maxId = -1;

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<DataDefinition>(assetPath);

                if (definition == null)
                {
                    continue;
                }

                maxId = Mathf.Max(maxId, definition.ID);
            }

            return maxId;
        }

        private void Persist()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
    }
}
#endif