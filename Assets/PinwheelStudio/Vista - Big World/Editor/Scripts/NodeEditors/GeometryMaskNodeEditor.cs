#if VISTA
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(GeometryMaskNode))]
    public class GeometryMaskNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent BLEND_MODE = new GUIContent("Blend Mode", "How to blend the mask");

        private static readonly GUIContent HEIGHT_MASK_HEADER = new GUIContent("Height");
        private static readonly GUIContent ENABLE_HEIGHT_MASK = new GUIContent("Enable");
        private static readonly GUIContent MIN_HEIGHT = new GUIContent("Min Height", "Minimum height level");
        private static readonly GUIContent MAX_HEIGHT = new GUIContent("Max Height", "Maximum height level");
        private static readonly GUIContent HEIGHT_TRANSITION = new GUIContent("Transition", "A curve to remap the mask");

        private static readonly GUIContent SLOPE_MASK_HEADER = new GUIContent("Slope");
        private static readonly GUIContent ENABLE_SLOPE_MASK = new GUIContent("Enable");
        private static readonly GUIContent MIN_ANGLE = new GUIContent("Min Angle", "Minimum surface angle in degree");
        private static readonly GUIContent MAX_ANGLE = new GUIContent("Max Angle", "Maximum surface angle in degree");
        private static readonly GUIContent SLOPE_TRANSITION = new GUIContent("Transition", "A curve to remap the mask");

        private static readonly GUIContent DIRECTION_MASK_HEADER = new GUIContent("Direction");
        private static readonly GUIContent ENABLE_DIRECTION_MASK = new GUIContent("Enable");
        private static readonly GUIContent DIRECTION = new GUIContent("Direction", "Target angle around Y-axis, in degree");
        private static readonly GUIContent DIRECTION_TOLERANCE = new GUIContent("Tolerance", "Expand the target angle");
        private static readonly GUIContent DIRECTION_FALLOFF = new GUIContent("Falloff", "Remap the mask");

        private static readonly Rect RECT01 = new Rect(0, 0, 1, 1);

        public override void OnGUI(INode node)
        {
            GeometryMaskNode n = node as GeometryMaskNode;
            GeometryMaskNode.BlendMode blendMode = n.blendMode;
            bool enableHeightMask = n.enableHeightMask;
            float minHeight = n.minHeight;
            float maxHeight = n.maxHeight;
            AnimationCurve heightTransition = new AnimationCurve(n.heightTransition.keys);

            bool enableSlopeMask = n.enableSlopeMask;
            float minAngle = n.minAngle;
            float maxAngle = n.maxAngle;
            AnimationCurve slopeTransition = new AnimationCurve(n.slopeTransition.keys);

            bool enableDirectionMask = n.enableDirectionMask;
            float direction = n.direction;
            float directionTolerance = n.directionTolerance;
            AnimationCurve directionFalloff = new AnimationCurve(n.directionFalloff.keys);

            EditorGUI.BeginChangeCheck();
            blendMode = (GeometryMaskNode.BlendMode)EditorGUILayout.EnumPopup(BLEND_MODE, n.blendMode);

            EditorCommon.Header(HEIGHT_MASK_HEADER);
            enableHeightMask = EditorGUILayout.Toggle(ENABLE_HEIGHT_MASK, n.enableHeightMask);
            if (enableHeightMask)
            {
                minHeight = EditorGUILayout.FloatField(MIN_HEIGHT, n.minHeight);
                maxHeight = EditorGUILayout.FloatField(MAX_HEIGHT, n.maxHeight);
                heightTransition = EditorGUILayout.CurveField(HEIGHT_TRANSITION, n.heightTransition, Color.red, RECT01);
            }

            EditorCommon.Header(SLOPE_MASK_HEADER);
            enableSlopeMask = EditorGUILayout.Toggle(ENABLE_SLOPE_MASK, n.enableSlopeMask);
            if (enableSlopeMask)
            {
                minAngle = EditorGUILayout.Slider(MIN_ANGLE, n.minAngle, 0f, 90f);
                maxAngle = EditorGUILayout.Slider(MAX_ANGLE, n.maxAngle, 0f, 90f);
                slopeTransition = EditorGUILayout.CurveField(SLOPE_TRANSITION, n.slopeTransition, Color.red, RECT01);
            }

            EditorCommon.Header(DIRECTION_MASK_HEADER);
            enableDirectionMask = EditorGUILayout.Toggle(ENABLE_DIRECTION_MASK, n.enableDirectionMask);
            if (enableDirectionMask)
            {
                direction = EditorGUILayout.Slider(DIRECTION, n.direction, 0f, 360f);
                directionTolerance = EditorGUILayout.Slider(DIRECTION_TOLERANCE, n.directionTolerance, 0f, 180f);
                directionFalloff = EditorGUILayout.CurveField(DIRECTION_FALLOFF, n.directionFalloff, Color.red, RECT01);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.blendMode = blendMode;
                n.enableHeightMask = enableHeightMask;
                n.minHeight = minHeight;
                n.maxHeight = maxHeight;
                n.heightTransition = heightTransition;

                n.enableSlopeMask = enableSlopeMask;
                n.minAngle = minAngle;
                n.maxAngle = maxAngle;
                n.slopeTransition = slopeTransition;

                n.enableDirectionMask = enableDirectionMask;
                n.direction = direction;
                n.directionTolerance = directionTolerance;
                n.directionFalloff = directionFalloff;
            }
        }
    }
}
#endif
