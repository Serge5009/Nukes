using UnityEngine;
using System.Collections.Generic;


//  A ScriptableObject that stores the vertex points for a single, continuous
//  border line, wrapped around the surface of the globe.

[CreateAssetMenu(fileName = "New Region Border Data", menuName = "Game Data/Region Border Data")]
public class RegionBorderData : ScriptableObject
{
    // This must be a Vector3 array to hold the 3D points from the raycast extractor.
    public Vector3[] borderPoints;
}
