using UnityEngine;


//  A ScriptableObject that holds all the data for a single region.
//  This allows to create data assets in the project folder.

[CreateAssetMenu(fileName = "New Region Data", menuName = "Game Data/Region Data")]
public class RegionData : ScriptableObject
{
    [Tooltip("The display name of the region.")]
    public string regionName;

    [Tooltip("The unique color used to identify this region on the ID Map.")]
    public Color32 regionIDColor; // Using Color32 for precise 0-255 values

    [Header("Ownership")]
    [Tooltip("The country that currently owns this region.")]
    public CountryData ownerCountry; 

    [Header("Gameplay Properties")]
    public long population;
    public float economyValue;

}
