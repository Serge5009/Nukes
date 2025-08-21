using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

/// <summary>
/// Handles player interaction with the globe. Detects clicks on regions
/// by using a raycast and looking up the color on an ID Map texture.
/// </summary>
public class RegionSelector : MonoBehaviour
{
    [Header("Required Components")]
    [Tooltip("The main camera used for raycasting.")]
    public Camera mainCamera;
    [Tooltip("The ID Map texture where each region has a unique color.")]
    public Texture2D idMapTexture;
    [Tooltip("A LayerMask to ensure the raycast only hits the globe.")]
    public LayerMask globeLayer;

    /// <summary>
    /// This method is called by the Player Input component when the "Click" action is performed.
    /// </summary>
    public void OnClick(InputAction.CallbackContext context)
    {
        // We only want to fire the raycast when the button is pressed down, not held or released.
        if (context.performed)
        {
            // Get the current mouse position from the new Input System.
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            DetectRegionClick(mousePosition);
        }
    }

    private void DetectRegionClick(Vector2 screenPosition)
    {
        // Create a ray from the camera through the mouse position.
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Perform the raycast.
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, globeLayer))
        {
            // Get the UV coordinate of the point where the ray hit the globe.
            Vector2 uv = hit.textureCoord;

            // Convert the UV coordinate to pixel coordinates on the ID Map.
            int x = (int)(uv.x * idMapTexture.width);
            int y = (int)(uv.y * idMapTexture.height);

            // Get the color of the pixel at that coordinate.
            Color32 clickedColor = idMapTexture.GetPixel(x, y);

            // --- Extended Debugging ---
            // This will print all the intermediate values to help diagnose the issue.
            Debug.Log($"Hit Object: {hit.collider.name} | UV Coords: ({uv.x:F3}, {uv.y:F3}) | Pixel Coords: ({x}, {y}) | Clicked Color: RGBA({clickedColor.r}, {clickedColor.g}, {clickedColor.b}, {clickedColor.a})");


            // Ask the RegionManager for the data associated with that color.
            RegionData selectedRegion = RegionManager.Instance.GetRegionData(clickedColor);

            // If a region was found, print its name to the console.
            if (selectedRegion != null)
            {
                Debug.Log($"SUCCESS! Clicked on region: {selectedRegion.regionName}");
            }
            else
            {
                // This can happen if you click on an area with no assigned region color (e.g., the ocean).
                Debug.Log("Clicked on an unassigned area.");
            }
        }
    }
}
