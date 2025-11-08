#if VISTA
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.Graph;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UInt16 = System.UInt16;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class SaveImage2dHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            TerrainGraphViewport2d.addUtilityButtonCallback += OnAddUtilityButton;
        }

        private static void OnAddUtilityButton(TerrainGraphViewport2d viewport, List<UtilityButton> buttons)
        {
            UtilityButton saveImageButton = new UtilityButton() { name = "save-image-2d-button" };
            saveImageButton.image = Resources.Load<Texture2D>("Vista/Textures/SaveImage2d");
            saveImageButton.tooltip = "Save 2D Viewport's content to .raw or .png file";
            saveImageButton.clicked += () => { SaveImage(viewport); };
            buttons.Add(saveImageButton);
        }

        private static void SaveImage(TerrainGraphViewport2d viewport)
        {
            RenderTexture rt = GetTargetTexture(viewport);
            if (rt == null)
                return;

            if (rt.format == RenderTextureFormat.RFloat)
            {
                SaveAsRaw16(rt);
            }
            else
            {
                SaveAsPng(rt);
            }
        }

        private static RenderTexture GetTargetTexture(TerrainGraphViewport2d viewport)
        {
            if (viewport.m_editor.m_lastExecution != null)
            {
                INode n = null;
                n = viewport.m_editor.clonedGraph.GetNode(viewport.m_editor.m_display2dNodeId);
                if (n == null)
                {
                    n = viewport.m_editor.clonedGraph.GetNode(viewport.m_editor.m_activeNodeId);
                }

                if (n != null)
                {
                    ISlot[] outputSlot = n.GetOutputSlots();
                    if (outputSlot.Length == 0)
                    {
                        return null;
                    }
                    else
                    {
                        string textureName = DataPool.GetName(n.id, outputSlot[0].id);
                        return viewport.m_editor.m_lastExecution.data.GetRT(textureName);
                    }
                }
            }
            return null;
        }

        private static void SaveAsRaw32(RenderTexture rt)
        {
            string path = EditorUtility.SaveFilePanel("Save as R32", "Assets/", "", "r32");
            if (string.IsNullOrEmpty(path))
                return;

            path = FileUtil.GetProjectRelativePath(path);

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, true);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] rawData = tex.GetRawTextureData();
            File.WriteAllBytes(path, rawData);

            Object.DestroyImmediate(tex);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RawImporter importer = AssetImporter.GetAtPath(path) as RawImporter;
            if (importer != null)
            {
                importer.width = rt.width;
                importer.height = rt.height;
                importer.bitDepth = RawImporter.BitDepth.Bit32;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                AssetDatabase.Refresh();
            }
        }

        private static void SaveAsRaw16(RenderTexture rt)
        {
            string path = EditorUtility.SaveFilePanel("Save as R16", "Assets/", "", "r16");
            if (string.IsNullOrEmpty(path))
                return;

            path = FileUtil.GetProjectRelativePath(path);

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, true);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            NativeArray<float> rawDataNative = tex.GetRawTextureData<float>();
            NativeArray<UInt16> r16Data = new NativeArray<UInt16>(rawDataNative.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < rawDataNative.Length; ++i)
            {
                UInt16 v = (UInt16)(UInt16.MaxValue * rawDataNative[i]);
                r16Data[i] = v;
            }

            File.WriteAllBytes(path, r16Data.Reinterpret<byte>(2).ToArray());
            r16Data.Dispose();
            rawDataNative.Dispose();

            Object.DestroyImmediate(tex);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RawImporter importer = AssetImporter.GetAtPath(path) as RawImporter;
            if (importer != null)
            {
                importer.width = rt.width;
                importer.height = rt.height;
                importer.bitDepth = RawImporter.BitDepth.Bit16;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                AssetDatabase.Refresh();
            }
        }

        private static void SaveAsPng(RenderTexture rt)
        {
            string path = EditorUtility.SaveFilePanel("Save as PNG", "Assets/", "", "png");
            if (string.IsNullOrEmpty(path))
                return;

            path = FileUtil.GetProjectRelativePath(path);

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] rawData = tex.EncodeToPNG();
            File.WriteAllBytes(path, rawData);

            Object.DestroyImmediate(tex);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
