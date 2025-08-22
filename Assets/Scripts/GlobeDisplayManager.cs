using UnityEngine;

public enum GlobeViewMode { Normal, Countries }

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
    [Tooltip("The pre-baked border map generated from the Editor tool.")]
    public Texture2D borderMap; // New field for your generated texture

    private Material globeMaterial;
    private Texture2D politicalMapTexture;

    void Start()
    {
        if (globeRenderer == null)
        {
            Debug.LogError("Globe Renderer is not assigned!", this);
            return;
        }

        globeMaterial = globeRenderer.material;

        // Assign the static border map to the shader at start.
        globeMaterial.SetTexture("_BorderMap", borderMap);

        GeneratePoliticalMap();
        SetViewMode(GlobeViewMode.Normal);
    }

    public void SetViewMode(GlobeViewMode mode)
    {
        globeMaterial.SetInt("_ViewMode", (int)mode);
        Debug.Log($"View Mode set to: {mode}");
    }

    public void GeneratePoliticalMap()
    {
        politicalMapTexture = new Texture2D(regionIDMap.width, regionIDMap.height, TextureFormat.RGB24, false);
        politicalMapTexture.filterMode = FilterMode.Point;

        Color32[] idMapPixels = regionIDMap.GetPixels32();
        Color32[] politicalMapPixels = new Color32[idMapPixels.Length];

        for (int i = 0; i < idMapPixels.Length; i++)
        {
            RegionData region = RegionManager.Instance.GetRegionData(idMapPixels[i]);
            if (region != null && region.ownerCountry != null)
            {
                politicalMapPixels[i] = region.ownerCountry.countryColor;
            }
            else
            {
                politicalMapPixels[i] = new Color32(0, 0, 0, 0); // Unowned areas are transparent
            }
        }

        politicalMapTexture.SetPixels32(politicalMapPixels);
        politicalMapTexture.Apply();

        globeMaterial.SetTexture("_PoliticalMap", politicalMapTexture);
        Debug.Log("Political map texture generated and sent to shader.");
    }
}
