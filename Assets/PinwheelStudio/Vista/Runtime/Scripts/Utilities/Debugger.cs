#if VISTA
using System.Collections.Generic;
using UnityEngine;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

public class Debugger : MonoBehaviour
{
    public static List<RenderTexture> renderTextures = new List<RenderTexture>();


    private void OnEnable()
    {
        renderTextures = new List<RenderTexture>();
    }

    private void OnDisable()
    {
        foreach (RenderTexture rt in renderTextures)
        {
            rt.Release();
            DestroyImmediate(rt);
        }
    }

    Vector2 scrollPos;

    private void OnGUI()
    {
        GUILayout.Label("Graphic device: " + SystemInfo.graphicsDeviceType);
        if (GUILayout.Button("Generate", GUILayout.Width(300), GUILayout.Height(150)))
        {
            LocalProceduralBiome[] biomes = FindObjectsByType<LocalProceduralBiome>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (LocalProceduralBiome biome in biomes)
            {
                biome.seed = Random.Range(0, 10000);
            }

            VistaManager vm = FindObjectOfType<VistaManager>();
            if (vm != null)
            {
                vm.GenerateAll();
            }
        }

        GUILayout.Label("RENDER TEXTURES");
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        foreach (RenderTexture rt in renderTextures)
        {
            if (rt.name.StartsWith("#"))
            {
                GUILayout.Label(rt.name.Substring(1).ToUpper());
            }
            else
            {
                GUILayout.BeginHorizontal();
                Rect imgRect = GUILayoutUtility.GetRect(100, 100);
                GUI.DrawTexture(imgRect, rt, ScaleMode.ScaleToFit, false);
                //GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(100));
                GUILayout.Label(rt.name);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Clear", GUILayout.Width(300), GUILayout.Height(80)))
        {
            foreach (RenderTexture rt in renderTextures)
            {
                rt.Release();
                DestroyImmediate(rt);
            }
            renderTextures.Clear();
        }
    }

    public static void RecordRT(RenderTexture src, string label)
    {
        RenderTexture rt = new RenderTexture(src);
        Drawing.Blit(src, rt);
        rt.name = label;
        renderTextures.Add(rt);
    }

    public static void RecordRT(Texture src, string label)
    {
        RenderTexture rt = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Drawing.Blit(src, rt);
        rt.name = label;
        renderTextures.Add(rt);
    }
}
#endif