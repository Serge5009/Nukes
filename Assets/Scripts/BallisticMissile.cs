using UnityEngine;

/// <summary>
/// Controls the flight of a ballistic missile along a three-phase trajectory:
/// 1. Curved Ascent (Quadratic Bezier)
/// 2. Arc Cruise (Spherical Linear Interpolation)
/// 3. Curved Descent (Quadratic Bezier)
/// </summary>


public class BallisticMissile : MonoBehaviour
{
    // Enum to manage the missile's flight state
    private enum FlightPhase { Idle, Ascent, Cruise, Descent, Impact }
    private FlightPhase currentPhase = FlightPhase.Idle;

    [Header("Trajectory Settings")]
    [Tooltip("The target the missile will fly towards.")]
    public Transform target;
    [Tooltip("The central body the missile orbits (e.g., Earth). Assumed to be at world origin.")]
    public Transform planet;
    [Tooltip("Cruise altitude in kilometers above the planet's surface.")]
    public float cruiseAltitude = 200f;
    [Tooltip("The ground distance from the launch site where the ascent phase should end (in km).")]
    public float ascentDistance = 500f;
    [Tooltip("The ground distance from the target where the descent phase should begin (in km).")]
    public float descentDistance = 500f;

    [Header("Speed Settings")]
    [Tooltip("Overall speed of the missile in kilometers per second.")]
    public float missileSpeedKps = 7f; // Realistic speed for an ICBM is ~7 km/s

    // Private fields for trajectory calculation
    private Vector3 launchPosition;
    private Vector3 targetPosition;

    // Phase variables
    private Vector3 ascentStartPoint, ascentEndPoint, ascentControlPoint;
    private Vector3 cruiseStartPoint, cruiseEndPoint;
    private Vector3 descentStartPoint, descentEndPoint, descentControlPoint;

    private float ascentProgress = 0f;
    private float cruiseProgress = 0f;
    private float descentProgress = 0f;

    // Dynamically calculated durations
    private float ascentDuration;
    private float cruiseDuration;
    private float descentDuration;

    private float planetRadius;



    private void Start()
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

        // --- Initial Setup ---
        // Assuming uniform scale for radius. Planet must be at world origin.
        planetRadius = planet.localScale.x / 2;
        launchPosition = transform.position;
        targetPosition = target.position;

        // --- Calculate Trajectory Points ---
        CalculateTrajectoryParameters();

        // --- Start the Flight ---
        currentPhase = FlightPhase.Ascent;
        Debug.Log("Missile Launched! Phase: Ascent");
    }

    void Update()
    {
        // State machine to handle the current flight phase
        switch (currentPhase)
        {
            case FlightPhase.Ascent:
                HandleAscent();
                break;
            case FlightPhase.Cruise:
                HandleCruise();
                break;
            case FlightPhase.Descent:
                HandleDescent();
                break;
            case FlightPhase.Impact:
                // Optionally, trigger an explosion or other effect here
                break;
        }
    }

    /// <summary>
    /// Pre-calculates all the necessary points and angles for the trajectory.
    /// </summary>
    private void CalculateTrajectoryParameters()
    {
        float cruiseRadius = planetRadius + cruiseAltitude;

        Vector3 launchDir = launchPosition.normalized;
        Vector3 targetDir = targetPosition.normalized;

        float totalAngleRad = Mathf.Acos(Vector3.Dot(launchDir, targetDir));

        // --- Define phase transition points based on distance ---
        float ascentAngleRad = ascentDistance / planetRadius;
        float descentAngleRad = descentDistance / planetRadius;

        float ascentEndProgress = Mathf.Clamp01(ascentAngleRad / totalAngleRad);
        float descentStartProgress = Mathf.Clamp01((totalAngleRad - descentAngleRad) / totalAngleRad);

        if (ascentEndProgress >= descentStartProgress)
        {
            float midPoint = (ascentEndProgress + descentStartProgress) / 2f;
            ascentEndProgress = midPoint;
            descentStartProgress = midPoint;
        }

        // --- Define Path Points ---
        ascentStartPoint = launchPosition;
        ascentEndPoint = Vector3.Slerp(launchDir, targetDir, ascentEndProgress).normalized * cruiseRadius;
        ascentControlPoint = Vector3.Slerp(ascentStartPoint, ascentEndPoint, 0.5f).normalized * cruiseRadius;

        cruiseStartPoint = ascentEndPoint;
        cruiseEndPoint = Vector3.Slerp(launchDir, targetDir, descentStartProgress).normalized * cruiseRadius;

        descentStartPoint = cruiseEndPoint;
        descentEndPoint = targetPosition;
        descentControlPoint = Vector3.Slerp(descentStartPoint, descentEndPoint, 0.5f).normalized * cruiseRadius;

        // --- Calculate Path Lengths ---
        float ascentLength = CalculateBezierLength(ascentStartPoint, ascentEndPoint, ascentControlPoint);
        float cruiseAngleRad = Mathf.Acos(Vector3.Dot(cruiseStartPoint.normalized, cruiseEndPoint.normalized));
        float cruiseLength = cruiseAngleRad * cruiseRadius;
        float descentLength = CalculateBezierLength(descentStartPoint, descentEndPoint, descentControlPoint);

        float totalDistance = ascentLength + cruiseLength + descentLength;

        // --- Calculate Durations based on Speed and Distance ---
        if (totalDistance > 0)
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

        ascentProgress += Time.deltaTime / ascentDuration;
        Vector3 newPosition = GetQuadraticBezierPoint(ascentStartPoint, ascentEndPoint, ascentControlPoint, ascentProgress);

        if (transform.position != newPosition)
            transform.rotation = Quaternion.LookRotation(newPosition - transform.position);

        transform.position = newPosition;

        if (ascentProgress >= 1.0f)
        {
            currentPhase = FlightPhase.Cruise;
            Debug.Log("Ascent Complete. Phase: Cruise");
        }
    }

    private void HandleCruise()
    {
        if (cruiseDuration <= 0) { currentPhase = FlightPhase.Descent; return; }

        cruiseProgress += Time.deltaTime / cruiseDuration;
        Vector3 newPosition = Vector3.Slerp(cruiseStartPoint, cruiseEndPoint, cruiseProgress);

        if (transform.position != newPosition)
            transform.rotation = Quaternion.LookRotation(newPosition - transform.position);

        transform.position = newPosition;

        if (cruiseProgress >= 1.0f)
        {
            currentPhase = FlightPhase.Descent;
            Debug.Log("Cruise Complete. Phase: Descent");
        }
    }

    private void HandleDescent()
    {
        if (descentDuration <= 0) { currentPhase = FlightPhase.Impact; return; }

        descentProgress += Time.deltaTime / descentDuration;
        Vector3 newPosition = GetQuadraticBezierPoint(descentStartPoint, descentEndPoint, descentControlPoint, descentProgress);

        if (transform.position != newPosition)
            transform.rotation = Quaternion.LookRotation(newPosition - transform.position);

        transform.position = newPosition;

        if (descentProgress >= 1.0f)
        {
            currentPhase = FlightPhase.Impact;
            Debug.Log("Impact!");
            // Destroy(gameObject);
        }
    }

    /// <summary>
    /// Approximates the length of a quadratic Bézier curve by summing line segments.
    /// </summary>
    private float CalculateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, int segments = 20)
    {
        float length = 0f;
        Vector3 previousPoint = p0;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 currentPoint = GetQuadraticBezierPoint(p0, p1, p2, t);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        return length;
    }

    /// <summary>
    /// Helper function to calculate a point on a quadratic Bézier curve.
    /// </summary>

    private Vector3 GetQuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return (oneMinusT * oneMinusT * p0) +
               (2f * oneMinusT * t * p2) +
               (t * t * p1);
    }
}
