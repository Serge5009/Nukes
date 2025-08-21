using UnityEngine;
using System.Collections.Generic;

//  An enum to clearly define the available map modes.
public enum GlobeViewMode { Normal, Regions, Countries }


//  Manages the visual representation of the globe, switching between different
//  map modes and generating textures at runtime.

public class GlobeDisplayManager : MonoBehaviour
{
    [Header("Core Components")]
    [Tooltip("The Mesh Renderer of your globe object.")]
    public Renderer globeRenderer;
    [Tooltip("The static ID Map texture used for region identification.")]
    public Texture2D regionIDMap;

    private Material globeMaterial;
    private Texture2D politicalMapTexture; //   This will be generated at runtime

    void Start()
    {
        if (globeRenderer == null)
        {
            Debug.LogError("Globe Renderer is not assigned!", this);
            return;
        }
        //  Get a unique instance of the material to avoid changing the project asset.
        globeMaterial = globeRenderer.material;

        //  Generate the initial political map when the game starts.
        GeneratePoliticalMap();
        //  Set the default view to the normal Earth texture.
        SetViewMode(0);
    }


    //  The main public method to change the globe's appearance.

    public void SetViewMode(int modeID)
    {
        GlobeViewMode mode = (GlobeViewMode)modeID;
        //  These property names ("_OverlayTexture", "_ViewMode") must match
        //  the names you use in your custom shader.
        switch (mode)
        {
            case GlobeViewMode.Normal:
                // Mode 0: Shader should be set to ignore the overlay texture.
                globeMaterial.SetInt("_ViewMode", 0);
                Debug.Log("View Mode set to: Normal");
                break;

            case GlobeViewMode.Regions:
                // Mode 1: Shader uses the Region ID Map as the overlay.
                globeMaterial.SetTexture("_OverlayTexture", regionIDMap);
                globeMaterial.SetInt("_ViewMode", 1);
                Debug.Log("View Mode set to: Regions");
                break;

            case GlobeViewMode.Countries:
                // Mode 2: Shader uses the dynamically generated Political Map as the overlay.
                globeMaterial.SetTexture("_OverlayTexture", politicalMapTexture);
                globeMaterial.SetInt("_ViewMode", 2);
                Debug.Log("View Mode set to: Countries");
                break;
        }
    }


    //  Generates a new texture representing the current political ownership of regions.
    //  This is a slow process and should only be called when borders change.

    public void GeneratePoliticalMap()
    {
        //  Create a new texture with the same dimensions as the ID map.
        politicalMapTexture = new Texture2D(regionIDMap.width, regionIDMap.height, TextureFormat.RGB24, false);
        politicalMapTexture.filterMode = FilterMode.Point; // Use Point filter for sharp, unaliased colors.

        //  Read all pixel data from the ID map at once for performance.
        Color32[] idMapPixels = regionIDMap.GetPixels32();
        Color32[] politicalMapPixels = new Color32[idMapPixels.Length];

        //  Loop through every pixel to create the new map.
        for (int i = 0; i < idMapPixels.Length; i++)
        {
            RegionData region = RegionManager.Instance.GetRegionData(idMapPixels[i]);
            if (region != null && region.ownerCountry != null)
            {
                //  If the region has an owner, use the country's color.
                politicalMapPixels[i] = region.ownerCountry.countryColor;
            }
            else
            {
                //  Otherwise, use the original ID map color (e.g., for oceans or unowned land).
                politicalMapPixels[i] = idMapPixels[i];
            }
        }

        //  Apply the new pixel data to the texture.
        politicalMapTexture.SetPixels32(politicalMapPixels);
        politicalMapTexture.Apply();
        Debug.Log("Political map texture generated.");
    }
}
