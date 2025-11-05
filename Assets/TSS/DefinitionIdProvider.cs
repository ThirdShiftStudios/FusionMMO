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
            ResetLastAssignedId();

            var guids = AssetDatabase.FindAssets($"t:{nameof(DataDefinition)}");
            var definitions = new List<(DataDefinition definition, string path)>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                var definition = AssetDatabase.LoadAssetAtPath<DataDefinition>(assetPath);
                if (definition == null)
                    continue;

                definitions.Add((definition, assetPath));
            }

            var orderedDefinitions = definitions
                .OrderBy(d => d.path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nextId = 0;

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

            if (nextId > 0)
            {
                lastAssignedId = nextId - 1;
                Persist();
            }
            else
            {
                ResetLastAssignedId();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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