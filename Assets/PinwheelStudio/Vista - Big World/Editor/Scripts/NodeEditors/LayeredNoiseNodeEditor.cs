#if VISTA
using Pinwheel.Vista.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(LayeredNoiseNode))]
    public class LayeredNoiseNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent BASE_SCALE = new GUIContent("Base Scale", "Scale of the first layer.");
        private static readonly GUIContent SEED = new GUIContent("Seed", "An integer to randomize the result");
        private static readonly GUIContent MODE = new GUIContent("Mode", "Noise mode");
        private static readonly GUIContent STRENGTH = new GUIContent("Strength", "Amplitude multiplier for this layer");

        private static readonly GUIContent ADD_LAYER = new GUIContent("Add Layer", "Add a new layer");

        public override void OnGUI(INode node)
        {
            LayeredNoiseNode n = node as LayeredNoiseNode;
            EditorGUI.BeginChangeCheck();
            float baseScale = EditorGUILayout.FloatField(BASE_SCALE, n.baseScale);
            int seed = EditorGUILayout.IntField(SEED, n.seed);

            List<LayeredNoiseNode.LayerConfig> layers = n.layers;
            for (int i = 0; i < layers.Count; ++i)
            {
                LayeredNoiseNode.LayerConfig l = layers[i];
                EditorGUILayout.BeginFoldoutHeaderGroup(true, $"Layer {i}", null, (r) => { ShowConfigContextMenu(n, r, l); });
                EditorGUI.BeginChangeCheck();
                NoiseMode mode = (NoiseMode)EditorGUILayout.EnumPopup(MODE, l.mode);
                float strength = EditorGUILayout.Slider(STRENGTH, l.strength, 0f, 1f);

                if (EditorGUI.EndChangeCheck())
                {
                    m_graphEditor.RegisterUndo(n);
                    l.mode = mode;
                    l.strength = strength;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (GUILayout.Button(ADD_LAYER))
            {
                LayeredNoiseNode.LayerConfig c = new LayeredNoiseNode.LayerConfig();
                c.mode = NoiseMode.Perlin01;
                c.strength = 0;

                if (layers.Count > 0)
                {
                    LayeredNoiseNode.LayerConfig lastLayer = layers[layers.Count - 1];
                    if (lastLayer != null)
                    {
                        c.mode = lastLayer.mode;
                        c.strength = lastLayer.strength * 0.5f;
                    }
                }

                layers.Add(c);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.baseScale = baseScale;
                n.seed = seed;
            }
        }

        private void ShowConfigContextMenu(LayeredNoiseNode node, Rect r, LayeredNoiseNode.LayerConfig c)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Remove"),
                false,
                () => { node.layers.Remove(c);});
            menu.DropDown(r);
            GUI.changed = true;
        }
    }
}
#endif
