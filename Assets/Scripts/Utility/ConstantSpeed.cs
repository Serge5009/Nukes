using UnityEngine;

public class ConstantSpeed : MonoBehaviour
{
    public Vector3 localVelocity;
    public float slowDownSpeed;


    void Update()
    {
        // Only update if the object is actually moving
        if (localVelocity.magnitude > 0.001f)
        {
            // --- Deceleration ---
            // This logic is the same as the original, but now applied to the local velocity.
            float currentMagnitude = localVelocity.magnitude;
            float newMagnitude = Mathf.Max(0, currentMagnitude - slowDownSpeed * Time.deltaTime);
            localVelocity = localVelocity.normalized * newMagnitude;

            // --- Position Update ---
            // 1. Calculate the movement vector for this frame in local space.
            Vector3 localMovementThisFrame = localVelocity * Time.deltaTime;

            // 2. Convert the local movement into a world-space direction.
            // This is the key step. It takes the object's rotation into account.
            Vector3 worldMovementThisFrame = transform.TransformDirection(localMovementThisFrame);

            // 3. Apply the world-space movement to the object's position, just like the original script.
            transform.position += worldMovementThisFrame;
        }
    }
}
