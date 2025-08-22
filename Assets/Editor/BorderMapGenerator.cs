using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// An Editor script that generates a high-quality, anti-aliased border map by
/// vectorizing the contours of a source ID map and then rendering clean lines.
/// </summary>
public class BorderMapGenerator : EditorWindow
{
    public Texture2D sourceIDMap;
    public float lineThickness = 1.5f;
    public Color lineColor = Color.black;

    [MenuItem("Tools/Border Map Generator (Vectorized)")]
    public static void ShowWindow()
    {
        GetWindow<BorderMapGenerator>("Border Map Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Vectorized Border Map Generation", EditorStyles.boldLabel);
        sourceIDMap = (Texture2D)EditorGUILayout.ObjectField("Source ID Map", sourceIDMap, typeof(Texture2D), false);
        lineThickness = EditorGUILayout.Slider("Line Thickness", lineThickness, 0.5f, 10f);
        lineColor = EditorGUILayout.ColorField("Line Color", lineColor);

        if (GUILayout.Button("Generate Border Map"))
        {
            if (sourceIDMap != null)
            {
                GenerateVectorizedBorderTexture();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Source ID Map.", "OK");
            }
        }
    }

    private void GenerateVectorizedBorderTexture()
    {
        // --- Step 1: Ensure texture is readable ---
        string sourcePath = AssetDatabase.GetAssetPath(sourceIDMap);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(sourcePath);
        if (!ti.isReadable)
        {
            ti.isReadable = true;
            ti.SaveAndReimport();
        }

        int width = sourceIDMap.width;
        int height = sourceIDMap.height;
        Color32[] sourcePixels = sourceIDMap.GetPixels32();

        // --- Step 2: Trace the contours to find border pixels ---
        List<Vector2Int> borderPixels = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 centerColor = sourcePixels[y * width + x];
                // Check 4 neighbors (no need for a full grid)
                if ((x > 0 && !sourcePixels[y * width + (x - 1)].Equals(centerColor)) ||
                    (x < width - 1 && !sourcePixels[y * width + (x + 1)].Equals(centerColor)) ||
                    (y > 0 && !sourcePixels[(y - 1) * width + x].Equals(centerColor)) ||
                    (y < height - 1 && !sourcePixels[(y + 1) * width + x].Equals(centerColor)))
                {
                    borderPixels.Add(new Vector2Int(x, y));
                }
            }
        }

        // --- Step 3: Render the vectorized points onto a new texture ---
        Texture2D outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] outputPixels = new Color32[width * height];
        // Initialize with a fully transparent background
        for (int i = 0; i < outputPixels.Length; i++)
        {
            outputPixels[i] = new Color32(0, 0, 0, 0);
        }

        // Draw anti-aliased circles at each border point to create a continuous line
        foreach (Vector2Int p in borderPixels)
        {
            DrawAntiAliasedCircle(outputPixels, width, height, p.x, p.y, lineThickness, lineColor);
        }

        // --- Step 4: Save the final texture ---
        outputTexture.SetPixels32(outputPixels);
        outputTexture.Apply();

        byte[] bytes = outputTexture.EncodeToPNG();
        string filePath = EditorUtility.SaveFilePanelInProject("Save Vectorized Border Map", "GeneratedVectorBorderMap", "png", "Please enter a file name");

        if (filePath.Length != 0)
        {
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Vectorized border map generated successfully!", "OK");
        }
    }

    // Draws a filled, anti-aliased circle onto the pixel array.
    // This is a common technique for drawing smooth points and lines.
    void DrawAntiAliasedCircle(Color32[] pixels, int width, int height, int centerX, int centerY, float radius, Color color)
    {
        int r_int = Mathf.CeilToInt(radius);
        for (int y = -r_int; y <= r_int; y++)
        {
            for (int x = -r_int; x <= r_int; x++)
            {
                int currentX = centerX + x;
                int currentY = centerY + y;

                if (currentX >= 0 && currentX < width && currentY >= 0 && currentY < height)
                {
                    float dist = Mathf.Sqrt(x * x + y * y);
                    // 'saturate' clamps the value between 0 and 1
                    float alpha = 1.0f - Mathf.Clamp01(dist - radius + 0.5f);

                    if (alpha > 0)
                    {
                        int index = currentY * width + currentX;
                        Color32 existingColor = pixels[index];
                        // Blend the new color with the existing one based on the new alpha
                        Color newColor = Color.Lerp(existingColor, color, alpha);
                        // Ensure we don't reduce the alpha of an already drawn pixel
                        newColor.a = Mathf.Max((float)existingColor.a / 255f, alpha);
                        pixels[index] = newColor;
                    }
                }
            }
        }
    }
}
