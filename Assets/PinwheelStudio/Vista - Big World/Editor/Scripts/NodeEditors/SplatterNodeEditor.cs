#if VISTA
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(SplatterNode))]
    public class SplatterNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent ROTATION_HEADER = new GUIContent("Rotation");
        private static readonly GUIContent MIN_ROTATION = new GUIContent("Min Rotation", "Min tree rotation in degree");
        private static readonly GUIContent MAX_ROTATION = new GUIContent("Max Rotation", "Max tree rotation in degree");
        private static readonly GUIContent ROTATION_MAP_MULTIPLIER = new GUIContent("Rotation Multiplier", "Adjust the rotation map");

        private static readonly GUIContent SIZE_HEADER = new GUIContent("Size");
        private static readonly GUIContent SIZE = new GUIContent("Size", "Size of the splat images");
        private static readonly GUIContent SCALE_MAP_MULTIPLIER = new GUIContent("Scale Multiplier", "Adjust the scale map");
        private static readonly GUIContent SIZE_IN_WORLD_SPACE = new GUIContent("World Space", "Is the size in world space or texture space");

        private static readonly GUIContent INTENSITY_HEADER = new GUIContent("Intensity");
        private static readonly GUIContent INTENSITY_MULTIPLIER = new GUIContent("Intensity Multiplier", "Adjust the intensity of the mask");

        public override void OnGUI(INode node)
        {
            SplatterNode n = node as SplatterNode;
            EditorGUI.BeginChangeCheck();
            EditorCommon.Header(ROTATION_HEADER);
            float minRotation = EditorGUILayout.Slider(MIN_ROTATION, n.minRotation, -360f, 360f);
            float maxRotation = EditorGUILayout.Slider(MAX_ROTATION, n.maxRotation, -360f, 360f);
            float rotationMultiplier = EditorGUILayout.Slider(ROTATION_MAP_MULTIPLIER, n.rotationMultiplier, 0f, 1f);

            EditorCommon.Header(SIZE_HEADER);
            float size = EditorGUILayout.FloatField(SIZE, n.size);
            float scaleMultiplier = EditorGUILayout.Slider(SCALE_MAP_MULTIPLIER, n.scaleMultiplier, 0f, 1f);
            bool sizeInWorldSpace = EditorGUILayout.Toggle(SIZE_IN_WORLD_SPACE, n.sizeInWorldSpace);

            EditorCommon.Header(INTENSITY_HEADER);
            float intensityMultiplier = EditorGUILayout.Slider(INTENSITY_MULTIPLIER, n.intensityMultiplier, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.minRotation = minRotation;
                n.maxRotation = maxRotation;
                n.rotationMultiplier = rotationMultiplier;
                n.size = size;
                n.scaleMultiplier = scaleMultiplier;
                n.sizeInWorldSpace = sizeInWorldSpace;
                n.intensityMultiplier = intensityMultiplier;
            }
        }
    }
}
#endif
