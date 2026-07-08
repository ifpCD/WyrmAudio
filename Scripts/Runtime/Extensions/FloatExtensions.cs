using UnityEngine;

public static class FloatExtensions
{
    public static float WithVariation(this float value, float amount = 0.05f)
    {
        return value + Random.Range(-amount, amount);
    }
}