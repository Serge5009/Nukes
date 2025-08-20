using UnityEngine;
using System.Collections.Generic;


//  A manager that loads all RegionData assets and provides a lookup
//  to find a region based on its ID color.

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance { get; private set; }

    private Dictionary<Color32, RegionData> regionLookup;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // --- Load all RegionData assets ---
        regionLookup = new Dictionary<Color32, RegionData>();
        // This assumes the RegionData assets are in a "Resources/RegionData" folder
        RegionData[] allRegions = Resources.LoadAll<RegionData>("RegionData");

        foreach (RegionData region in allRegions)
        {
            if (!regionLookup.ContainsKey(region.regionIDColor))
            {
                regionLookup.Add(region.regionIDColor, region);
            }
            else
            {
                Debug.LogWarning($"Duplicate color key found for region: {region.name}. Check your RegionData assets.");
            }
        }
        Debug.Log($"Loaded {regionLookup.Count} regions into the manager.");
    }


    //  Gets RegionData based on a color from the ID Map.
    //  Returns null if no region is found for the given color.

    public RegionData GetRegionData(Color32 color)
    {
        regionLookup.TryGetValue(color, out RegionData data);
        return data;
    }
}
