using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Loads all pre-calculated 3D border data and combines it into a single, optimized mesh.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BorderRenderer : MonoBehaviour
{
    [Header("Rendering Settings")]
    [Tooltip("The material to use for the border lines. A simple unlit color material works best.")]
    public Material lineMaterial;

    private Mesh borderMesh;

    void Start()
    {
        // --- 1. Load all the border data assets ---
        RegionBorderData[] allBorders = Resources.LoadAll<RegionBorderData>("BorderData");

        if (allBorders.Length == 0)
        {
            Debug.LogWarning("No border data found. Make sure you have run the Border Extractor and the assets are in a Resources/BorderData folder.");
            return;
        }

        // --- 2. Combine all 3D points into lists for the new mesh ---
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        foreach (RegionBorderData borderData in allBorders)
        {
            // For each line segment in the border...
            for (int i = 0; i < borderData.borderPoints.Length - 1; i++)
            {
                vertices.Add(borderData.borderPoints[i]);
                vertices.Add(borderData.borderPoints[i + 1]);

                indices.Add(vertices.Count - 2);
                indices.Add(vertices.Count - 1);
            }
        }

        // --- 3. Create and apply the new mesh ---
        borderMesh = new Mesh();
        borderMesh.name = "Combined Border Mesh";
        if (vertices.Count > 65535)
        {
            borderMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        borderMesh.SetVertices(vertices);
        borderMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
        borderMesh.RecalculateBounds();

        // --- 4. Assign the mesh and material to this object ---
        GetComponent<MeshFilter>().mesh = borderMesh;
        GetComponent<MeshRenderer>().material = lineMaterial;
    }
}
