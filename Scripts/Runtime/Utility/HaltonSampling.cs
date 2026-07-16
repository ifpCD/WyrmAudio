using System;
using System.Collections.Generic;
using UnityEngine;

internal static class Sampling
{
    public static void GenerateBoxVolumeSamples(BoxCollider box, int numSamples, List<Vector3> samples)
    {
        samples.Clear();
        for (int i = 0; i < numSamples; ++i)
        {
            float u = RadicalInverse(2, i);
            float v = RadicalInverse(3, i);
            float w = RadicalInverse(5, i);

            Vector3 local = new(
                Mathf.Lerp(-box.size.x * 0.5f, box.size.x * 0.5f, u),
                Mathf.Lerp(-box.size.y * 0.5f, box.size.y * 0.5f, v),
                Mathf.Lerp(-box.size.z * 0.5f, box.size.z * 0.5f, w)
            );

            local += box.center;

            samples.Add(box.transform.TransformPoint(local));
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
