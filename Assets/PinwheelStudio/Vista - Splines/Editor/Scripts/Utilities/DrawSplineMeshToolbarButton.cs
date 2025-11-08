#if VISTA
#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor.Toolbars;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Pinwheel.VistaEditor.Splines;
using System;
using Pinwheel.Vista.Splines;

namespace Pinwheel.VistaEditor.UIElements
{
    [EditorToolbarElement(DrawSplineMeshToolbarToggle.ID, typeof(SceneView))]
    public class DrawSplineMeshToolbarToggle : EditorToolbarToggle
    {
        public const string ID = "vista-draw-spline-mesh-button";

        public DrawSplineMeshToolbarToggle()
        {
            text = "";
            onIcon = Resources.Load<Texture2D>("Vista/Textures/SplineMesh");
            offIcon = Resources.Load<Texture2D>("Vista/Textures/SplineMesh");
            tooltip = "Draw spline mesh gizmos";
            value = SplineModuleEditorUtilities.drawSplineMeshInSceneView;

            SceneView.duringSceneGui += DuringSceneViewGUI;
        }

        protected override void ToggleValue()
        {
            base.ToggleValue();
            SplineModuleEditorUtilities.drawSplineMeshInSceneView = this.value;
        }

        private void DuringSceneViewGUI(SceneView sv)
        {
            this.value = SplineModuleEditorUtilities.drawSplineMeshInSceneView;

            if (!SplineModuleEditorUtilities.drawSplineMeshInSceneView)
                return;
            if (Event.current.type != EventType.Repaint)
                return;

            ISplineEvaluator[] splines = SplineModuleUtilities.GetAllSplines();
            foreach (ISplineEvaluator s in splines)
            {
                if (s is ITrianglesBufferProvider tbp)
                {
                    DrawSplineMesh(sv, tbp);
                }
            }
        }

        private static class DrawConfig
        {
            public static readonly string SHADER = "Hidden/Vista/Splines/SplineMesh";
            public static readonly int TRIANGLES_BUFFER = Shader.PropertyToID("_TrianglesBuffer");

            private static Material s_material;
            public static Material material
            {
                get
                {
                    if (s_material == null)
                    {
                        s_material = new Material(Shader.Find(SHADER));
                    }
                    return s_material;
                }
            }
        }

        private void DrawSplineMesh(SceneView sv, ITrianglesBufferProvider tbp)
        {
            ComputeBuffer triangleBuffer = tbp.trianglesBuffer;
            if (triangleBuffer == null)
                return;

            Camera cam = sv.camera;
            Bounds bounds = new Bounds(cam.transform.position, Vector3.one * 10000);

            Material mat = DrawConfig.material;
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetBuffer(DrawConfig.TRIANGLES_BUFFER, triangleBuffer);

            Graphics.DrawProcedural(mat, bounds, MeshTopology.Lines, triangleBuffer.count * 2, 1, cam, props, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0);
        }
    }
}
#endif
#endif