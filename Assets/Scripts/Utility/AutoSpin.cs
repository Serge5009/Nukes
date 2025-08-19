using UnityEngine;

public class AutoSpin : MonoBehaviour
{
    public float spinX;
    public float spinY;
    public float spinZ;

    Vector3 startingRotation;

    [Range(0, 5.0f)] public float randomization = 0;

    private void Start()
    {
        startingRotation = new Vector3(spinX, spinY, spinZ);
    }

    void Update()
    {
        if(Random.Range(0, 60) == 9)
        {
            if (spinX != 0)
                spinX *= Random.Range(1, 1 + randomization);
            if (spinY != 0)
                spinY *= Random.Range(1, 1 + randomization);
            if (spinZ != 0)
                spinZ *= Random.Range(1, 1 + randomization);
        }
        if (Random.Range(0, 600) == 9)
        {
            spinX = startingRotation.x;
            spinY = startingRotation.y;
            spinZ = startingRotation.z;
        }

            transform.Rotate(new Vector3(spinX, spinY, spinZ) * Time.deltaTime);
    }
}
