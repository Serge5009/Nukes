using UnityEngine;

/// <summary>
/// Controls the flight of a ballistic missile along a three-phase trajectory.
/// This version includes dynamic altitude for short-range targets and improved curve calculations.
/// </summary>
public class BallisticMissile : MonoBehaviour
{
    // Enum to manage the missile's flight state
    private enum FlightPhase { Idle, Ascent, Cruise, Descent, Impact }
    private FlightPhase currentPhase = FlightPhase.Idle;

    [Header("Trajectory Settings")]
    public Transform target;
    public Transform planet;
    [Tooltip("Maximum cruise altitude for long-range shots (in km).")]
    public float maxCruiseAltitude = 200f;
    [Tooltip("The ground distance from launch where ascent ends (in km).")]
    public float ascentDistance = 500f;
    [Tooltip("The ground distance from the target where descent begins (in km).")]
    public float descentDistance = 500f;

    [Header("Speed Settings")]
    [Tooltip("Overall speed of the missile in km/s at a time scale of 1.")]
    public float missileSpeedKps = 7f;

    [Header("Explosion")]
    [SerializeField] GameObject explosionPrefab;


    // Private trajectory data
    private Vector3 ascentStartPoint, ascentEndPoint, ascentControlPoint;
    private Vector3 cruiseStartPoint, cruiseEndPoint;
    private Vector3 descentStartPoint, descentEndPoint, descentControlPoint;

    private float ascentProgress = 0f;
    private float cruiseProgress = 0f;
    private float descentProgress = 0f;

    private float ascentDuration, cruiseDuration, descentDuration;
    private float planetRadius;

    /// <summary>
    /// For testing purposes, automatically launches the missile on start.
    /// </summary>
    void Start()
    {
        Launch();
    }

    public void Launch()
    {
        if (target == null || planet == null)
        {
            Debug.LogError("Missile is missing a Target or Planet reference!", this);
            return;
        }

        planetRadius = planet.localScale.x / 2;
        CalculateTrajectoryParameters();
        currentPhase = FlightPhase.Ascent;
        Debug.Log("Missile Launched!");
    }

    void Update()
    {
        switch (currentPhase)
        {
            case FlightPhase.Ascent: HandleAscent(); break;
            case FlightPhase.Cruise: HandleCruise(); break;
            case FlightPhase.Descent: HandleDescent(); break;
        }
    }

    private void CalculateTrajectoryParameters()
    {
        Vector3 launchPos = transform.position;
        Vector3 targetPos = target.position;
        Vector3 launchDir = launchPos.normalized;
        Vector3 targetDir = targetPos.normalized;

        float totalAngleRad = Mathf.Acos(Vector3.Dot(launchDir, targetDir));
        float totalGroundDist = totalAngleRad * planetRadius;

        // --- Dynamic Altitude for Short-Range Targets ---
        float altitudeScaleFactor = Mathf.Clamp01(totalGroundDist / (ascentDistance + descentDistance));
        float dynamicCruiseAltitude = maxCruiseAltitude * altitudeScaleFactor;
        float cruiseRadius = planetRadius + dynamicCruiseAltitude;

        // --- Define Phase Transition Points ---
        float ascentAngleRad = ascentDistance / planetRadius;
        float descentAngleRad = descentDistance / planetRadius;
        float ascentEndProgress = Mathf.Clamp01(ascentAngleRad / totalAngleRad);
        float descentStartProgress = Mathf.Clamp01(1f - (descentAngleRad / totalAngleRad));

        if (ascentEndProgress >= descentStartProgress)
        {
            float midPoint = (ascentEndProgress + descentStartProgress) / 2f;
            ascentEndProgress = midPoint;
            descentStartProgress = midPoint;
        }

        // --- Define Path Points ---
        ascentStartPoint = launchPos;
        ascentEndPoint = Vector3.Slerp(launchDir, targetDir, ascentEndProgress).normalized * cruiseRadius;
        ascentControlPoint = Vector3.Slerp(ascentStartPoint, ascentEndPoint, 0.5f).normalized * cruiseRadius;

        cruiseStartPoint = ascentEndPoint;
        cruiseEndPoint = Vector3.Slerp(launchDir, targetDir, descentStartProgress).normalized * cruiseRadius;

        descentStartPoint = cruiseEndPoint;
        descentEndPoint = targetPos;

        descentControlPoint = Vector3.Slerp(descentStartPoint, descentEndPoint, 0.5f).normalized * cruiseRadius;


        // --- Calculate Path Lengths and Durations ---
        float ascentLength = CalculateBezierLength(ascentStartPoint, ascentEndPoint, ascentControlPoint);
        float cruiseLength = Vector3.Angle(cruiseStartPoint, cruiseEndPoint) * Mathf.Deg2Rad * cruiseRadius;
        float descentLength = CalculateBezierLength(descentStartPoint, descentEndPoint, descentControlPoint);

        float totalDistance = ascentLength + cruiseLength + descentLength;

        if (totalDistance > 0 && missileSpeedKps > 0)
        {
            float totalDuration = totalDistance / missileSpeedKps;
            ascentDuration = totalDuration * (ascentLength / totalDistance);
            cruiseDuration = totalDuration * (cruiseLength / totalDistance);
            descentDuration = totalDuration * (descentLength / totalDistance);
        }
    }

    private void HandleAscent()
    {
        if (ascentDuration <= 0) { currentPhase = FlightPhase.Cruise; return; }
        ascentProgress += CustomTime.deltaTime / ascentDuration;
        UpdatePositionAndRotation(ascentStartPoint, ascentEndPoint, ascentControlPoint, ascentProgress);
        if (ascentProgress >= 1.0f) { currentPhase = FlightPhase.Cruise; }
    }

    private void HandleCruise()
    {
        if (cruiseDuration <= 0) { currentPhase = FlightPhase.Descent; return; }
        cruiseProgress += CustomTime.deltaTime / cruiseDuration;
        Vector3 newPosition = Vector3.Slerp(cruiseStartPoint, cruiseEndPoint, cruiseProgress);
        UpdateRotation(newPosition);
        transform.position = newPosition;
        if (cruiseProgress >= 1.0f) { currentPhase = FlightPhase.Descent; }
    }

    private void HandleDescent()
    {
        if (descentDuration <= 0) { currentPhase = FlightPhase.Impact; return; }
        descentProgress += CustomTime.deltaTime / descentDuration;
        UpdatePositionAndRotation(descentStartPoint, descentEndPoint, descentControlPoint, descentProgress);
        if (descentProgress >= 1.0f)
        {
            currentPhase = FlightPhase.Impact;
            Impact();
        }
    }

    void Impact()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    // --- Helper Methods ---

    private void UpdatePositionAndRotation(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        Vector3 newPosition = GetQuadraticBezierPoint(p0, p1, p2, t);
        UpdateRotation(newPosition);
        transform.position = newPosition;
    }

    private void UpdateRotation(Vector3 newPosition)
    {
        Vector3 direction = newPosition - transform.position;

        // Only update rotation if there is a meaningful direction to look at
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // **FIX** Smoothly turn towards the target rotation instead of snapping
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 180.0f);
        }
    }


    private float CalculateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, int segments = 20)
    {
        float length = 0f;
        Vector3 previousPoint = p0;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 currentPoint = GetQuadraticBezierPoint(p0, p1, p2, (float)i / segments);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        return length;
    }

    private Vector3 GetQuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return (oneMinusT * oneMinusT * p0) + (2f * oneMinusT * t * p2) + (t * t * p1);
    }
}