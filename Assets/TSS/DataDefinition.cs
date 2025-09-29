// Assets/TSS/DataDefinitions/DataDefinition.cs
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TSS.Data
{
    public abstract class DataDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private int id = -1;
        public int ID => id;

        public abstract string Name { get; }
        public abstract Texture2D Icon { get; }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (id < 0)
            {
                id = DefinitionIdProvider.instance.GetNextIdAndPersist();
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}