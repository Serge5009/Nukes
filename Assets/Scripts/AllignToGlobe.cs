using UnityEngine;

public class AllignToGlobe : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The planet Transform to align this object to. Assumed to be a sphere at the world origin.")]
    public GameObject planet;

    [Tooltip("If true, the object will automatically align itself when the game starts.")]
    public bool alignOnStart = true;

    //  On start, align the object to the globe if the setting is enabled.
    void Start()
    {
        if (!planet)
            planet = GameManager.gameManager.planetModel;
        if (!planet)
            Debug.LogError("AllignToGlobe failed to get the globe");



        if (alignOnStart)
        {
            Align();
        }
    }

    //  Projects the object onto the planet's surface and aligns its rotation.
    public void Align()
    {
        if (planet == null)
        {
            Debug.LogWarning("Planet not assigned. Cannot align object.", this);
            return;
        }

        // --- Position Alignment ---
        // Calculate the planet's radius (assuming uniform scale and origin at 0,0,0)
        float planetRadius = planet.transform.localScale.x / 2f;

        // Get the direction from the planet's center to this object's current position
        Vector3 directionFromCenter = transform.position.normalized;

        // Set the object's position to be on the surface of the planet
        transform.position = directionFromCenter * planetRadius;


        // --- Rotation Alignment ---
        // Calculate the rotation that makes the object's local "up" (Y axis)
        // point away from the planet's center.
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, directionFromCenter) * transform.rotation;

        // Apply the new rotation
        transform.rotation = targetRotation;
    }
}
