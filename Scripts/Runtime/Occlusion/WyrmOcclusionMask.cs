using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public partial class WyrmOcclusionMask : AmbiMonoBehaviour<WyrmOcclusionMask>
{
    [SerializeField]
    OcclusionMaskType _type = OcclusionMaskType.Singular;

    public const int MAX_SAMPLE_COUNT = 64;

    [Range(1, MAX_SAMPLE_COUNT)]
    [SerializeField]
    int _sampleCount = 1;

    [Range(0.2f, 10f)]
    [SerializeField]
    float _size = 1f;

    float Radius => _size / 2;

    List<Vector3> _haltonBuffer = new(64);

    List<OcclusionSample> _occlusionSamples = new(64);

    void OnEnable()
    {
        Register();
        RegisterSamples();
    }

    void OnDisable()
    {
        Deregister();
        DeregisterSamples();
    }

    void OnValidate()
    {
        RebuildSampleBuffer();
        RegisterSamples();
        SoASync();
    }

    void RebuildSampleBuffer()
    {
        _occlusionSamples.Clear();

        if (_type is OcclusionMaskType.SphericalHalton)
        {
            HaltonSequence.GenerateSphereVolumeSamples(Radius, _sampleCount, _haltonBuffer);

            for (var i = 0; i < _sampleCount; i++)
            {
                var newSample = new OcclusionSample { LocalPosition = _haltonBuffer[i] };
                _occlusionSamples.Add(newSample);
            }
        }
    }

    void RegisterSamples()
    {
        if (!IsRegistered)
            return;

        foreach (var sample in _occlusionSamples)
        {
            sample.Register();
            OcclusionSample.MaskOwnerIndices[sample.SoAIndex] = SoAIndex;
        }
    }

    void DeregisterSamples()
    {
        foreach (var sample in _occlusionSamples)
        {
            sample.Deregister();
            OcclusionSample.MaskOwnerIndices[sample.SoAIndex] = SoAIndex;
        }
    }
}

enum OcclusionMaskType : byte
{
    Singular,
    SphericalHalton,
    Custom,
}
