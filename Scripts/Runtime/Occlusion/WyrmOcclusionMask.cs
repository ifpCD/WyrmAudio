using System.Collections.Generic;
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

    readonly List<Vector3> _generatedPositionsBuffer = new(MAX_SAMPLE_COUNT);

    readonly List<WyrmOcclusionSample> _sampleBuffer = new(MAX_SAMPLE_COUNT);

    void OnEnable()
    {
        if (_sampleBuffer.Count == 0)
            RebuildSampleBuffer();
            
        Register();
        RegisterSamples();
    }

    void OnDisable()
    {
        DeregisterSamples();
        Deregister();
    }

    void OnValidate()
    {
        DeregisterSamples();
        RebuildSampleBuffer();
        RegisterSamples();

        SoASync();
    }

    void RebuildSampleBuffer()
    {
        _sampleBuffer.Clear();
        _generatedPositionsBuffer.Clear();

        if (_type is OcclusionMaskType.SphericalHalton or OcclusionMaskType.Singular)
        {
            if (_type is OcclusionMaskType.SphericalHalton)
                HaltonSequence.GenerateSphereVolumeSamples(Radius, _sampleCount, _generatedPositionsBuffer);
            else
                _generatedPositionsBuffer.Add(Vector3.zero);

            for (var i = 0; i < _generatedPositionsBuffer.Count; i++)
            {
                var newSample = new WyrmOcclusionSample { LocalPosition = _generatedPositionsBuffer[i] };
                _sampleBuffer.Add(newSample);
            }
        }
    }

    void RegisterSamples()
    {
        if (!IsRegistered)
            return;

        foreach (var sample in _sampleBuffer)
        {
            sample.Register();
            WyrmOcclusionSample.SampleToMask[sample.SoAIndex] = SoAIndex;
        }
    }

    void DeregisterSamples()
    {
        if (!IsRegistered)
            return;

        foreach (var sample in _sampleBuffer)
            sample.Deregister();
    }
}

enum OcclusionMaskType : byte
{
    Singular,
    SphericalHalton,
    Custom,
}
