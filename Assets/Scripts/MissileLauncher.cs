using UnityEngine;


//  Manages the launching of ballistic missiles at a set interval.
//  Can be configured to fire at a fixed target or at random locations on a planet.
//  Reload time is scaled by CustomTime.

public class MissileLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    [Tooltip("The missile prefab to be launched.")]
    public GameObject missilePrefab;
    [Tooltip("The fixed target for the missile. Ignored if targeting is random.")]
    public Transform target;
    [Tooltip("The planet the launcher is on. Used for random targeting.")]
    public Transform planet;
    [Tooltip("The time in seconds between each missile launch.")]
    public float reloadTime = 10f;

    [Header("Targeting")]
    [Tooltip("If true, the launcher will ignore the fixed target and fire at random locations on the planet's surface.")]
    public bool targetRandomLocation = false;

    private float reloadTimer;

    void Start()
    {
        // Basic validation to ensure all required components are assigned.
        if (missilePrefab == null || planet == null)
        {
            Debug.LogError("Missile Launcher is missing a Missile Prefab or Planet reference!", this);
            return;
        }
        if (!targetRandomLocation && target == null)
        {
            Debug.LogError("A fixed target must be assigned if not targeting random locations!", this);
            return;
        }

        // Initialize the timer to start the countdown for the first launch.
        reloadTimer = reloadTime;
    }

    void Update()
    {
        // Count down the timer using the scalable CustomTime.
        reloadTimer -= CustomTime.deltaTime;

        // If the timer has reached zero, fire a missile and reset.
        if (reloadTimer <= 0f)
        {
            FireMissile();
            reloadTimer = reloadTime;
        }
    }


    //  Handles the logic for firing a single missile.
    private void FireMissile()
    {
        Transform currentTarget = target;
        GameObject tempTargetObject = null;

        //  If targeting is random, create a temporary target object.
        if (targetRandomLocation)
        {
            tempTargetObject = new GameObject("RandomTarget (Temporary)");
            float planetRadius = planet.localScale.x / 2f;
            // Get a random point on the surface of a sphere.
            tempTargetObject.transform.position = Random.onUnitSphere * planetRadius;
            currentTarget = tempTargetObject.transform;
        }

        //  Instantiate the missile at the launcher's position and rotation.
        GameObject missileInstance = Instantiate(missilePrefab, transform.position, Quaternion.identity);
        BallisticMissile missile = missileInstance.GetComponent<BallisticMissile>();

        if (missile != null)
        {
            // Assign the missile's parameters and launch it.
            missile.target = currentTarget;
            missile.planet = planet;
            missile.Launch();
        }
        else
        {
            Debug.LogError("The assigned missile prefab does not have a BallisticMissile component!", this);
        }

        // If a temporary target was created, destroy it after a short delay.
        // This gives the missile time to copy the target's position.
        if (tempTargetObject != null)
        {
            Destroy(tempTargetObject, 1f);
        }
    }
}
