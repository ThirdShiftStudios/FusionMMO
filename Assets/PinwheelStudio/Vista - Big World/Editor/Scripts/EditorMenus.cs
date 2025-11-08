#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor;
using Pinwheel.Vista.Graph;
using System.IO;

namespace Pinwheel.VistaEditor.BigWorld
{
    public static class EditorMenus
    {
        [MenuItem("Assets/Create/Vista/Biome Mask Graph", priority = -9999)]
        public static void CreateNewBiomeMaskGraph()
        {
            string directory = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            string assetName = "New Biome Mask Graph.asset";
            string filePath = Path.Combine(directory, assetName);
            BiomeMaskGraph graph = ScriptableObject.CreateInstance<BiomeMaskGraph>();
            graph.name = assetName;

            InputNode inputNode = new InputNode();
            inputNode.inputName = GraphConstants.BIOME_MASK_INPUT_NAME;
            inputNode.SetSlotType(typeof(MaskSlot));
            inputNode.visualState = new VisualState() { position = Vector2.zero };

            OutputNode outputNode = new OutputNode();
            outputNode.outputName = GraphConstants.BIOME_MASK_OUTPUT_NAME;
            outputNode.SetSlotType(typeof(MaskSlot));
            outputNode.visualState = new VisualState() { position = Vector2.right * 1000f };

            Edge edge = new Edge(new SlotRef(inputNode.id, inputNode.outputSlot.id), new SlotRef(outputNode.id, outputNode.inputSlot.id));

            graph.AddNode(inputNode);
            graph.AddNode(outputNode);
            graph.AddEdge(edge);

            ProjectWindowUtil.CreateAsset(graph, filePath);
        }
    }
}
#endif
