// Assets/TSS/Editor/DefinitionIdProvider.cs
#if UNITY_EDITOR
using System;
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