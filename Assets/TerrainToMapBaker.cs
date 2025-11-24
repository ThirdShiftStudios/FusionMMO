#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

[ExecuteInEditMode]
public class TerrainToMapBaker : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Terrain _terrain;

    [Header("Output")]
    [SerializeField] private int _resolution = 1024;
    [SerializeField] private Gradient _heightGradient;
    [SerializeField] private string _savePath = "Assets/Maps/terrain_map.png";

    [Header("Height Range (optional override)")]
    [SerializeField] private bool _useCustomHeightRange = false;
    [SerializeField] private float _minHeight = 0f;
    [SerializeField] private float _maxHeight = 600f;

    [ContextMenu("Bake Map")]
    public void BakeMap()
    {
        if (_terrain == null)
        {
            Debug.LogError("TerrainToMapBaker: No Terrain assigned.");
            return;
        }

        TerrainData data = _terrain.terrainData;
        if (data == null)
        {
            Debug.LogError("TerrainToMapBaker: Terrain has no TerrainData.");
            return;
        }

        int res = Mathf.Max(16, _resolution);
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);

        // Get heights (Unity’s heightmap resolution is often different, so we sample ourselves)
        float worldSizeX = data.size.x;
        float worldSizeZ = data.size.z;

        float minH = _useCustomHeightRange ? _minHeight : 0f;
        float maxH = _useCustomHeightRange ? _maxHeight : data.size.y;
        float heightRange = Mathf.Max(0.0001f, maxH - minH);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                // Normalize to [0,1] across terrain
                float nx = (float)x / (res - 1);
                float nz = (float)y / (res - 1);

                // Map to world position
                float worldX = nx * worldSizeX;
                float worldZ = nz * worldSizeZ;

                // Sample from terrain heights (0–1) using normalized coords
                float normalizedX = worldX / worldSizeX;
                float normalizedZ = worldZ / worldSizeZ;
                float h01 = data.GetInterpolatedHeight(normalizedX, normalizedZ); // returns world-space height

                // Convert to [0,1] relative to chosen height range
                float normalizedHeight = Mathf.InverseLerp(minH, maxH, h01);

                // Gradient-based color
                Color c = _heightGradient != null
                    ? _heightGradient.Evaluate(normalizedHeight)
                    : Color.Lerp(Color.green, Color.white, normalizedHeight);

                // Y-flip so top of texture is +Z in world
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();

        // Ensure directory exists
        string directory = Path.GetDirectoryName(_savePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Encode to PNG and write
        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(_savePath, pngData);

        Debug.Log($"TerrainToMapBaker: Saved map to {_savePath}");

        // Import into Unity and configure as sprite if desired
        AssetDatabase.ImportAsset(_savePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(_savePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite; // so you can use it in UI Image
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Debug.Log("TerrainToMapBaker: Import complete, texture set as Sprite.");
    }
}
#endif
