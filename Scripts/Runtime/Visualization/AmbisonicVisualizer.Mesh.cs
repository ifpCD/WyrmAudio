#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class AmbisonicVisualizer
{
    // Uniform directions (no pole pinching); vertex normals carry the directions. Level 6: 40962 vertices, ~1.1 deg apart.
    static Mesh BuildIcosphere(int subdivisions)
    {
        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;

        var vertices = new List<Vector3>
        {
            new(-1f, t, 0f), new(1f, t, 0f), new(-1f, -t, 0f), new(1f, -t, 0f),
            new(0f, -1f, t), new(0f, 1f, t), new(0f, -1f, -t), new(0f, 1f, -t),
            new(t, 0f, -1f), new(t, 0f, 1f), new(-t, 0f, -1f), new(-t, 0f, 1f),
        };

        for (int index = 0; index < vertices.Count; index++)
            vertices[index] = vertices[index].normalized;

        var triangles = new List<int>
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        var midpoints = new Dictionary<long, int>();

        for (int level = 0; level < subdivisions; level++)
        {
            var subdivided = new List<int>(triangles.Count * 4);
            midpoints.Clear();

            for (int corner = 0; corner < triangles.Count; corner += 3)
            {
                int a = triangles[corner];
                int b = triangles[corner + 1];
                int c = triangles[corner + 2];

                int ab = Midpoint(a, b, vertices, midpoints);
                int bc = Midpoint(b, c, vertices, midpoints);
                int ca = Midpoint(c, a, vertices, midpoints);

                subdivided.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            triangles = subdivided;
        }

        // Unity front faces: cross(b - a, c - a) points outward
        for (int corner = 0; corner < triangles.Count; corner += 3)
        {
            Vector3 a = vertices[triangles[corner]];
            Vector3 b = vertices[triangles[corner + 1]];
            Vector3 c = vertices[triangles[corner + 2]];

            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
                (triangles[corner + 1], triangles[corner + 2]) = (triangles[corner + 2], triangles[corner + 1]);
        }

        var mesh = new Mesh { name = "Ambisonic Icosphere", hideFlags = HideFlags.DontSave };
        mesh.indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetNormals(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        return mesh;
    }

    static int Midpoint(int a, int b, List<Vector3> vertices, Dictionary<long, int> midpoints)
    {
        long edge = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

        if (midpoints.TryGetValue(edge, out int index))
            return index;

        index = vertices.Count;
        vertices.Add(((vertices[a] + vertices[b]) * 0.5f).normalized);
        midpoints.Add(edge, index);
        return index;
    }
}
#endif
