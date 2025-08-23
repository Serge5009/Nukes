using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// An Editor script to generate a high-quality Signed Distance Field (SDF) texture
/// from a raw, pixelated ID map using a Jump Flooding Algorithm for superior results.
/// </summary>
public class SdfMapGenerator : EditorWindow
{
    public Texture2D sourceIDMap;
    [Tooltip("How far out the distance field should be calculated. Higher is smoother.")]
    public int spread = 64;

    [MenuItem("Tools/SDF Map Generator (High Quality)")]
    public static void ShowWindow()
    {
        GetWindow<SdfMapGenerator>("SDF Map Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("High-Quality SDF Map Generation", EditorStyles.boldLabel);
        sourceIDMap = (Texture2D)EditorGUILayout.ObjectField("Source ID Map", sourceIDMap, typeof(Texture2D), false);
        spread = EditorGUILayout.IntSlider("Spread (px)", spread, 8, 128);

        if (GUILayout.Button("Generate SDF Map"))
        {
            if (sourceIDMap != null)
            {
                GenerateSdfTexture();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Source ID Map.", "OK");
            }
        }
    }

    private void GenerateSdfTexture()
    {
        // --- Step 1: Setup and Validation ---
        string sourcePath = AssetDatabase.GetAssetPath(sourceIDMap);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(sourcePath);
        if (!ti.isReadable || ti.textureCompression != TextureImporterCompression.Uncompressed)
        {
            ti.isReadable = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        int width = sourceIDMap.width;
        int height = sourceIDMap.height;
        Color32[] sourcePixels = sourceIDMap.GetPixels32();

        // --- Step 2: Initialize Seed Points for Jump Flooding ---
        Vector2[] seedPoints = new Vector2[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (IsBorder(sourcePixels, width, height, x, y))
                {
                    seedPoints[index] = new Vector2(x, y);
                }
                else
                {
                    seedPoints[index] = new Vector2(float.MaxValue, float.MaxValue);
                }
            }
        }

        // --- Step 3: Jump Flooding Algorithm ---
        int step = width / 2;
        while (step >= 1)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            int nx = x + i * step;
                            int ny = y + j * step;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                int nIndex = ny * width + nx;
                                if (Vector2.Distance(new Vector2(x, y), seedPoints[nIndex]) < Vector2.Distance(new Vector2(x, y), seedPoints[index]))
                                {
                                    seedPoints[index] = seedPoints[nIndex];
                                }
                            }
                        }
                    }
                }
            }
            step /= 2;
        }

        // --- Step 4: Calculate final distances and save texture ---
        Color[] outputPixels = new Color[width * height];
        for (int i = 0; i < seedPoints.Length; i++)
        {
            int x = i % width;
            int y = i / width;
            float dist = Vector2.Distance(new Vector2(x, y), seedPoints[i]);
            float normalized = Mathf.Clamp01(dist / spread);
            outputPixels[i] = new Color(normalized, normalized, normalized, 1);
        }

        Texture2D outputTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
        outputTexture.SetPixels(outputPixels);
        outputTexture.Apply();

        byte[] bytes = outputTexture.EncodeToEXR();
        string filePath = EditorUtility.SaveFilePanelInProject("Save SDF Map", "GeneratedSDFMap_HQ", "exr", "Please enter a file name");

        if (filePath.Length != 0)
        {
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "High-quality SDF map generated successfully!", "OK");
        }
    }

    #region Border Detection
    private bool ColorsAreDifferent(Color32 c1, Color32 c2)
    {
        return c1.r != c2.r || c1.g != c2.g || c1.b != c2.b;
    }

    private bool IsBorder(Color32[] pixels, int w, int h, int x, int y)
    {
        Color32 center = pixels[y * w + x];
        if (x > 0 && ColorsAreDifferent(pixels[y * w + (x - 1)], center)) return true;
        if (x < w - 1 && ColorsAreDifferent(pixels[y * w + (x + 1)], center)) return true;
        if (y > 0 && ColorsAreDifferent(pixels[(y - 1) * w + x], center)) return true;
        if (y < h - 1 && ColorsAreDifferent(pixels[(y + 1) * w + x], center)) return true;
        return false;
    }
    #endregion
}
