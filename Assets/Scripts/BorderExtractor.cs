using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;


/// Extracts border contours from an ID map by looking up the precise 3D world
/// coordinates from the globe's mesh data, ensuring perfect alignment.

public class BorderExtractor : EditorWindow
{
    public Texture2D sourceIDMap;
    public MeshCollider globeCollider; 
    private string saveFolderPath = "Assets/Resources/BorderData";

    [MenuItem("Tools/Border Extractor (Mesh Lookup)")]
    public static void ShowWindow()
    {
        GetWindow<BorderExtractor>("Border Extractor");
    }

    void OnGUI()
    {
        GUILayout.Label("Mesh Lookup Border Data Extraction", EditorStyles.boldLabel);
        sourceIDMap = (Texture2D)EditorGUILayout.ObjectField("Source ID Map", sourceIDMap, typeof(Texture2D), false);
        globeCollider = (MeshCollider)EditorGUILayout.ObjectField("Globe Mesh Collider", globeCollider, typeof(MeshCollider), true);
        saveFolderPath = EditorGUILayout.TextField("Save Folder Path", saveFolderPath);

        if (GUILayout.Button("Extract 3D Borders"))
        {
            if (sourceIDMap != null && globeCollider != null)
            {
                ExtractAndSaveBorders();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Source ID Map and a Globe Mesh Collider.", "OK");
            }
        }
    }

    private void ExtractAndSaveBorders()
    {
        // --- Step 1: Setup and Validation ---
        if (!Directory.Exists(saveFolderPath)) Directory.CreateDirectory(saveFolderPath);
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
        Color32[] pixels = sourceIDMap.GetPixels32();
        bool[,] visited = new bool[width, height];

        // --- Step 2: Trace all 2D Borders ---
        List<List<Vector2Int>> allContours = TraceAllContours(pixels, width, height, ref visited);

        // --- Step 3: Convert 2D Contours to 3D Points via Mesh Lookup ---
        Mesh globeMesh = globeCollider.sharedMesh;
        Vector3[] vertices = globeMesh.vertices;
        Vector2[] uvs = globeMesh.uv;
        int[] triangles = globeMesh.triangles;
        int count = 0;

        foreach (var contour in allContours)
        {
            RegionBorderData borderData = ScriptableObject.CreateInstance<RegionBorderData>();
            List<Vector3> points3D = new List<Vector3>();

            foreach (Vector2Int pixelCoord in contour)
            {
                Vector2 targetUV = new Vector2((float)pixelCoord.x / width, (float)pixelCoord.y / height);
                Vector3? point = Find3DPointForUV(targetUV, vertices, uvs, triangles);
                if (point.HasValue)
                {
                    points3D.Add(globeCollider.transform.TransformPoint(point.Value));
                }
            }

            if (points3D.Count > 10)
            {
                borderData.borderPoints = points3D.ToArray();
                string assetPath = Path.Combine(saveFolderPath, $"Border_{count++}.asset");
                AssetDatabase.CreateAsset(borderData, assetPath);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Success", $"Successfully extracted {count} 3D borders.", "OK");
    }

    private Vector3? Find3DPointForUV(Vector2 targetUV, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            Vector2 uv1 = uvs[i1];
            Vector2 uv2 = uvs[i2];
            Vector2 uv3 = uvs[i3];

            Vector3 barycentric = GetBarycentric(targetUV, uv1, uv2, uv3);
            if (barycentric.x >= 0 && barycentric.y >= 0 && barycentric.z >= 0) // Point is in triangle
            {
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];
                return barycentric.x * v1 + barycentric.y * v2 + barycentric.z * v3;
            }
        }
        return null; // Should not happen if UVs are correct
    }

    private Vector3 GetBarycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;
        return new Vector3(u, v, w);
    }

    #region Contour Tracing
    private List<List<Vector2Int>> TraceAllContours(Color32[] pixels, int w, int h, ref bool[,] visited)
    {
        List<List<Vector2Int>> allContours = new List<List<Vector2Int>>();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!visited[x, y] && IsBorder(pixels, w, h, x, y))
                {
                    List<Vector2Int> contour = TraceContour(pixels, w, h, new Vector2Int(x, y), ref visited);
                    if (contour.Count > 10) allContours.Add(contour);
                }
            }
        }
        return allContours;
    }

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

    private List<Vector2Int> TraceContour(Color32[] pixels, int w, int h, Vector2Int start, ref bool[,] visited)
    {
        List<Vector2Int> contour = new List<Vector2Int>();
        Vector2Int current = start;
        Vector2Int prev = new Vector2Int(start.x - 1, start.y);
        Vector2Int[] directions = {
            new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1)
        };
        do
        {
            contour.Add(current);
            visited[current.x, current.y] = true;
            int startIndex = System.Array.FindIndex(directions, d => d == prev - current);
            bool foundNext = false;
            for (int i = 0; i < 8; i++)
            {
                int dirIndex = (startIndex + i + 1) % 8;
                Vector2Int next = current + directions[dirIndex];
                if (next.x >= 0 && next.x < w && next.y >= 0 && next.y < h && IsBorder(pixels, w, h, next.x, next.y))
                {
                    prev = current;
                    current = next;
                    foundNext = true;
                    break;
                }
            }
            if (!foundNext) break;
        } while (current != start && contour.Count < 20000);
        return contour;
    }
    #endregion
}
