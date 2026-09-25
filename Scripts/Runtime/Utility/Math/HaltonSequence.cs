using System;
using System.Collections.Generic;
using UnityEngine;

internal static class HaltonSequence
{
    public static void GenerateBoxVolumeSamples(BoxCollider box, int numSamples, List<Vector3> buffer)
    {
        Vector3 size = box.size;

        GenerateBoxVolumeSamples(size, numSamples, buffer);
    }

    public static void GenerateBoxVolumeSamples(Vector3 size, int numSamples, List<Vector3> buffer)
    {
        buffer.Clear();
        for (int i = 0; i < numSamples; ++i)
        {
            float u = RadicalInverse(2, i);
            float v = RadicalInverse(3, i);
            float w = RadicalInverse(5, i);

            Vector3 local = new(
                Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, u),
                Mathf.Lerp(-size.y * 0.5f, size.y * 0.5f, v),
                Mathf.Lerp(-size.z * 0.5f, size.z * 0.5f, w)
            );

            buffer.Add(local);
        }
    }

    public static void GenerateSphereVolumeSamples(SphereCollider sphere, int numSamples, List<Vector3> buffer)
    {
        float radius = sphere.radius;

        GenerateSphereVolumeSamples(radius, numSamples, buffer);
    }

    public static void GenerateSphereVolumeSamples(float radius, int numSamples, List<Vector3> buffer)
    {
        buffer.Clear();

        for (int i = 0; i < numSamples; ++i)
        {
            float u = RadicalInverse(2, i);
            float v = RadicalInverse(3, i);
            float w = RadicalInverse(5, i);

            float r = radius * Mathf.Pow(u, 1.0f / 3.0f);
            float theta = 2.0f * Mathf.PI * v;

            float phi = Mathf.Acos(1.0f - 2.0f * w);

            // csharpier-ignore
            Vector3 local = new(
                r * Mathf.Sin(phi) * Mathf.Cos(theta),
                r * Mathf.Cos(phi),
                r * Mathf.Sin(phi) * Mathf.Sin(theta)
            );

            buffer.Add(local);
        }
    }

    public static float RadicalInverse(int p, int i)
    {
        float inv = 1.0f / p;
        int reversed = 0;
        float invN = 1.0f;

        while (i > 0)
        {
            int next = i / p;
            int digit = i - (next * p);
            reversed = reversed * p + digit;
            invN *= inv;
            i = next;
        }

        return Mathf.Min(reversed * invN, 1.0f);
    }
}
