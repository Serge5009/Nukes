using UnityEngine;
using System.Collections.Generic;


//  A ScriptableObject that holds all the data for a single country.

[CreateAssetMenu(fileName = "New Country Data", menuName = "Game Data/Country Data")]
public class CountryData : ScriptableObject
{
    [Tooltip("The display name of the country.")]
    public string countryName;

    [Tooltip("The color used to represent this country on the political map mode.")]
    public Color32 countryColor;
}
