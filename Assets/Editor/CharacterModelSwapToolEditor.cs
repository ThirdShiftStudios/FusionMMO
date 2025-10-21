#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterModelSwapTool))]
public class CharacterModelSwapToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Assign the current and new model hierarchies, then click the button below to remap references and migrate missing objects.", MessageType.Info);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Apply Character Model Swap"))
            {
                foreach (var targetObject in targets)
                {
                    if (targetObject is CharacterModelSwapTool tool)
                    {
                        tool.ApplySwap();
                    }
                }
            }
        }
    }
}
#endif
