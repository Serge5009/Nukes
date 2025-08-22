using UnityEngine;
using System.Collections.Generic;

// An enum to clearly define the available map modes.
public enum GlobeViewMode { Normal, Regions, Countries }

/// <summary>
/// Manages the visual representation of the globe, switching between different
/// map modes and generating textures at runtime.
/// </summary>
public class GlobeDisplayManager : MonoBehaviour
{
    [Header("Core Components")]
    [Tooltip("The Mesh Renderer of your globe object.")]
    public Renderer globeRenderer;
    [Tooltip("The static ID Map texture used for region identification.")]
    public Texture2D regionIDMap;

    private Material globeMaterial;
    private Texture2D politicalMapTexture; // This will be generated at runtime

    void Start()
    {
        if (globeRenderer == null)
        {
            Debug.LogError("Globe Renderer is not assigned!", this);
            return;
        }
        // Get a unique instance of the material to avoid changing the project asset.
        globeMaterial = globeRenderer.material;

        // **CRITICAL:** Assign the static region map to the shader at start.
        globeMaterial.SetTexture("_RegionIDMap", regionIDMap);

        // Generate the initial political map when the game starts.
        GeneratePoliticalMap();
        // Set the default view to the normal Earth texture.
        SetViewMode(GlobeViewMode.Normal);
    }

    /// <summary>
    /// The main public method to change the globe's appearance.
    /// </summary>
    public void SetViewMode(GlobeViewMode mode)
    {
        // The C# script only needs to tell the shader which mode to be in.
        // The shader itself handles which textures to read based on this mode.
        globeMaterial.SetInt("_ViewMode", (int)mode);
        Debug.Log($"View Mode set to: {mode}");
    }

    /// <summary>
    /// Generates a new texture representing the current political ownership of regions.
    /// This is a slow process and should only be called when borders change.
    /// </summary>
    public void GeneratePoliticalMap()
    {
        // Create a new texture with the same dimensions as the ID map.
        politicalMapTexture = new Texture2D(regionIDMap.width, regionIDMap.height, TextureFormat.RGB24, false);
        politicalMapTexture.filterMode = FilterMode.Point; // Use Point filter for sharp, unaliased colors.

        // Read all pixel data from the ID map at once for performance.
        Color32[] idMapPixels = regionIDMap.GetPixels32();
        Color32[] politicalMapPixels = new Color32[idMapPixels.Length];

        // Loop through every pixel to create the new map.
        for (int i = 0; i < idMapPixels.Length; i++)
        {
            RegionData region = RegionManager.Instance.GetRegionData(idMapPixels[i]);
            if (region != null && region.ownerCountry != null)
            {
                // If the region has an owner, use the country's color.
                politicalMapPixels[i] = region.ownerCountry.countryColor;
            }
            else
            {
                // Otherwise, use the original ID map color (e.g., for oceans or unowned land).
                politicalMapPixels[i] = idMapPixels[i];
            }
        }

        // Apply the new pixel data to the texture.
        politicalMapTexture.SetPixels32(politicalMapPixels);
        politicalMapTexture.Apply();

        // Send the newly generated texture to the correct property in the shader.
        globeMaterial.SetTexture("_PoliticalMap", politicalMapTexture);

        Debug.Log("Political map texture generated and sent to shader.");
    }
}
