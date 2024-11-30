using UnityEngine;

public static class Easing
{
    public static float EaseOutSin(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }
}
