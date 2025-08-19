using UnityEngine;

public static class CustomTime
{
    public static float timeScale = 1.0f;

    public static float deltaTime
    {
        get { return Time.deltaTime * timeScale; }
    }

}
