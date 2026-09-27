#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed partial class AmbisonicVisualizer
{
    // low, mid, high, broadband
    const int ENERGY_VECTOR_COUNT = HC.MAX_AMBISONIC_BANDS + 1;

    NativeArray<float3> _speakerDirections;
    NativeArray<float4> _speakerFeeds;
    NativeArray<float4> _energyVectors;
    float _speakerPeak;

    readonly Vector4[] _speakerUpload = new Vector4[VirtualSpeakerLayout.COUNT];

    Mesh _speakerMesh;
    Material _speakerMaterial;
    MaterialPropertyBlock _speakerProperties;

    static readonly int SPEAKER_FEEDS_ID = Shader.PropertyToID("_SpeakerFeeds");
    static readonly int SPEAKER_PEAK_ID = Shader.PropertyToID("_SpeakerPeak");
    static readonly int SPEAKER_RADIUS_ID = Shader.PropertyToID("_SpeakerRadius");
    static readonly int SPEAKER_SIZE_ID = Shader.PropertyToID("_SpeakerSize");
    static readonly int SPEAKER_RANGE_ID = Shader.PropertyToID("_SpeakerRange");
    static readonly int SPEAKER_IDLE_OPACITY_ID = Shader.PropertyToID("_SpeakerIdleOpacity");

    void AllocateSpeakers()
    {
        _speakerDirections = new(VirtualSpeakerLayout.Directions, Allocator.Persistent);
        _speakerFeeds = new(VirtualSpeakerLayout.COUNT, Allocator.Persistent);
        _energyVectors = new(ENERGY_VECTOR_COUNT, Allocator.Persistent);
    }

    void DeallocateSpeakers()
    {
        _speakerDirections.TryDispose();
        _speakerFeeds.TryDispose();
        _energyVectors.TryDispose();
    }

    bool EnsureSpeakerResources()
    {
        _speakerProperties ??= new MaterialPropertyBlock();

        if (_speakerMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/WyrmAudio/AmbisonicVirtualSpeakers");

            if (shader == null)
                return false;

            _speakerMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        }

        if (_speakerMesh == null)
            _speakerMesh = BuildSpeakerMesh();

        return true;
    }

    void DecodeSpeakers(int order, quaternion headRotation)
    {
        new DecodeVirtualSpeakersJob
        {
            Order = order,
            HeadRotation = headRotation,
            SpeakerDirections = _speakerDirections,
            Field = _field,
            SpeakerFeeds = _speakerFeeds,
            EnergyVectors = _energyVectors,
        }.Run();

        _speakerPeak = 0f;

        for (int speaker = 0; speaker < VirtualSpeakerLayout.COUNT; speaker++)
        {
            float4 feed = _speakerFeeds[speaker];
            _speakerUpload[speaker] = feed;
            _speakerPeak = math.max(_speakerPeak, math.length(feed.xyz));
        }
    }

    void DrawSpeakers()
    {
        _speakerProperties.SetVectorArray(SPEAKER_FEEDS_ID, _speakerUpload);
        _speakerProperties.SetFloat(SPEAKER_PEAK_ID, _speakerPeak);
        _speakerProperties.SetFloat(SPEAKER_RADIUS_ID, speakerRadius);
        _speakerProperties.SetFloat(SPEAKER_SIZE_ID, speakerSize);
        _speakerProperties.SetFloat(SPEAKER_RANGE_ID, speakerRangeDecibels);
        _speakerProperties.SetFloat(SPEAKER_IDLE_OPACITY_ID, speakerIdleOpacity);

        _speakerProperties.SetColor(BASE_COLOR_ID, idleColor);
        _speakerProperties.SetColor(LOW_COLOR_ID, lowFreqColor);
        _speakerProperties.SetColor(MID_COLOR_ID, midFreqColor);
        _speakerProperties.SetColor(HIGH_COLOR_ID, highFreqColor);

        var headLocked = Matrix4x4.TRS(_listenerPosition, _listenerRotation, Vector3.one);
        Graphics.DrawMesh(_speakerMesh, headLocked, _speakerMaterial, gameObject.layer, null, 0, _speakerProperties);
    }

    // One octahedron per speaker in a single draw: uv0 carries the head-frame direction, uv1.x the speaker index.
    static Mesh BuildSpeakerMesh()
    {
        Vector3[] corners = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        int[] faces = { 0, 2, 4, 2, 1, 4, 1, 3, 4, 3, 0, 4, 2, 0, 5, 1, 2, 5, 3, 1, 5, 0, 3, 5 };

        var vertices = new List<Vector3>(VirtualSpeakerLayout.COUNT * corners.Length);
        var directions = new List<Vector3>(vertices.Capacity);
        var indices = new List<Vector2>(vertices.Capacity);
        var triangles = new List<int>(VirtualSpeakerLayout.COUNT * faces.Length);

        for (int speaker = 0; speaker < VirtualSpeakerLayout.COUNT; speaker++)
        {
            int first = vertices.Count;

            foreach (Vector3 corner in corners)
            {
                vertices.Add(corner);
                directions.Add(VirtualSpeakerLayout.Directions[speaker]);
                indices.Add(new Vector2(speaker, 0f));
            }

            for (int corner = 0; corner < faces.Length; corner += 3)
            {
                int a = faces[corner];
                int b = faces[corner + 1];
                int c = faces[corner + 2];

                // Unity front faces: cross(b - a, c - a) points outward
                if (Vector3.Dot(Vector3.Cross(corners[b] - corners[a], corners[c] - corners[a]), corners[a] + corners[b] + corners[c]) < 0f)
                    (b, c) = (c, b);

                triangles.Add(first + a);
                triangles.Add(first + b);
                triangles.Add(first + c);
            }
        }

        var mesh = new Mesh { name = "Ambisonic Virtual Speakers", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetNormals(vertices);
        mesh.SetUVs(0, directions);
        mesh.SetUVs(1, indices);
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        return mesh;
    }
}
#endif
