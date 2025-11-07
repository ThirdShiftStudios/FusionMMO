using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using Den.Tools;
using MapMagic.Nodes;
using MapMagic.Nodes.GUI;
using MapMagic.Nodes.MatrixGenerators;

namespace FusionMMO.MapMagicExtensions
{
    /// <summary>
    /// Editor utilities that can build a ready-to-use MapMagic 2 graph
    /// configured for mountainous terrains with erosion and terraces.
    /// The menu item creates the asset on demand, and a safety check also
    /// ensures that a default copy exists inside the project.
    /// </summary>
    public static class FusionTerrainGraphFactory
    {
        private const string DefaultGraphPath = "Assets/MapMagicExtensions/Graphs/Fusion Terrain Graph.asset";

        [InitializeOnLoadMethod]
        private static void EnsureDefaultGraph()
        {
            if (AssetDatabase.LoadAssetAtPath<Graph>(DefaultGraphPath) != null)
            {
                return;
            }

            CreateFolderIfNeeded(Path.GetDirectoryName(DefaultGraphPath));

            Graph graph = CreateGraph();
            graph.name = Path.GetFileNameWithoutExtension(DefaultGraphPath);
            AssetDatabase.CreateAsset(graph, DefaultGraphPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshGraphLookup();
        }

        [MenuItem("Assets/Create/FusionMMO/Terrain Graph", priority = 103)]
        private static void CreateGraphFromMenu()
        {
            Texture2D icon = TexturesCache.LoadTextureAtPath("MapMagic/Icons/AssetBig");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<CreateGraphCallback>(),
                "Fusion Terrain Graph.asset",
                icon,
                string.Empty);
        }

        private static void RefreshGraphLookup()
        {
            if (GraphInspector.allGraphsGuids == null)
            {
                GraphInspector.allGraphsGuids = new HashSet<string>();
            }

            GraphInspector.allGraphsGuids.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:Graph"))
            {
                GraphInspector.allGraphsGuids.Add(guid);
            }
        }

        private static void CreateFolderIfNeeded(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateFolderIfNeeded(parent);
            }

            string folderName = Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
        }

        private static Graph CreateGraph()
        {
            Graph graph = ScriptableObject.CreateInstance<Graph>();

            Noise200 continentNoise = (Noise200)Generator.Create(typeof(Noise200));
            graph.Add(continentNoise);
            continentNoise.guiPosition = new Vector2(-650f, -30f);
            continentNoise.type = Noise200.Type.Simplex;
            continentNoise.seed = 12842;
            continentNoise.intensity = 0.85f;
            continentNoise.size = 640f;
            continentNoise.detail = 0.25f;
            continentNoise.turbulence = 0.18f;

            Noise200 mountainNoise = (Noise200)Generator.Create(typeof(Noise200));
            graph.Add(mountainNoise);
            mountainNoise.guiPosition = new Vector2(-650f, 160f);
            mountainNoise.type = Noise200.Type.Perlin;
            mountainNoise.seed = 65431;
            mountainNoise.intensity = 0.55f;
            mountainNoise.size = 160f;
            mountainNoise.detail = 0.7f;
            mountainNoise.turbulence = 0.45f;

            Blend200 blend = (Blend200)Generator.Create(typeof(Blend200));
            graph.Add(blend);
            blend.guiPosition = new Vector2(-420f, 60f);
            blend.layers = new Blend200.Layer[]
            {
                new Blend200.Layer { algorithm = Blend200.BlendAlgorithm.add, opacity = 1f },
                new Blend200.Layer { algorithm = Blend200.BlendAlgorithm.add, opacity = 0.35f }
            };
            foreach (Blend200.Layer layer in blend.layers)
            {
                layer.inlet.SetGen(blend);
            }
            graph.Link(blend.layers[0].inlet, continentNoise);
            graph.Link(blend.layers[1].inlet, mountainNoise);

            Curve200 curve = (Curve200)Generator.Create(typeof(Curve200));
            graph.Add(curve);
            curve.guiPosition = new Vector2(-220f, 60f);
            curve.curve = new Curve(new Curve.Node[]
            {
                new Curve.Node(new Vector2(0f, 0f)) { linear = true },
                new Curve.Node(new Vector2(0.25f, 0.08f)) { linear = true },
                new Curve.Node(new Vector2(0.55f, 0.68f)),
                new Curve.Node(new Vector2(0.82f, 0.92f)),
                new Curve.Node(new Vector2(1f, 1f)) { linear = true }
            });
            curve.curve.Refresh();
            graph.Link(curve, blend);

            Terrace200 terrace = (Terrace200)Generator.Create(typeof(Terrace200));
            graph.Add(terrace);
            terrace.guiPosition = new Vector2(-20f, 60f);
            terrace.num = 7;
            terrace.uniformity = 0.65f;
            terrace.steepness = 0.6f;
            terrace.seed = 22711;
            graph.Link(terrace, curve);

            Erosion200 erosion = (Erosion200)Generator.Create(typeof(Erosion200));
            graph.Add(erosion);
            erosion.guiPosition = new Vector2(180f, 60f);
            erosion.iterations = 48;
            erosion.terrainDurability = 0.62f;
            erosion.erosionAmount = 0.88f;
            erosion.sedimentAmount = 0.72f;
            erosion.fluidityIterations = 4;
            erosion.relax = 0.18f;
            graph.Link(erosion, terrace);

            HeightOutput200 heightOut = (HeightOutput200)Generator.Create(typeof(HeightOutput200));
            graph.Add(heightOut);
            heightOut.guiPosition = new Vector2(390f, 60f);
            graph.Link(heightOut, erosion);

            return graph;
        }

        private class CreateGraphCallback : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Graph graph = CreateGraph();
                graph.name = Path.GetFileNameWithoutExtension(pathName);
                AssetDatabase.CreateAsset(graph, pathName);
                AssetDatabase.SaveAssets();
                ProjectWindowUtil.ShowCreatedAsset(graph);
                RefreshGraphLookup();
            }
        }
    }
}
