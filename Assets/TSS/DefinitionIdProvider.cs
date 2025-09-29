// Assets/TSS/Editor/DefinitionIdProvider.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TSS.Data
{
    [FilePath("ProjectSettings/TSS_DefinitionIdProvider.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class DefinitionIdProvider : ScriptableSingleton<DefinitionIdProvider>
    {
        public int LastAssignedId = -1;

        public int GetNextIdAndPersist()
        {
            unchecked
            {
                LastAssignedId = LastAssignedId < -1 ? -1 : LastAssignedId;
                LastAssignedId++;
            }

            // Save() is protected; OK to call here.
            Save(true);
            return LastAssignedId;
        }
    }
}
#endif