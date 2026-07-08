using UnityEngine;

public static class FloatExtensions
{
    public static float Deviate(this float value, float amount = 0.05f)
    {
        return value + Random.Range(-amount, amount);
    }
}