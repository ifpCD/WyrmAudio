using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[Serializable]
public struct MaskSampleData
{
    public Vector3 LocalPosition;
    public float Weight;
    public bool Discardable;
}

[NoAutoStaticsCleanup]
public partial class WyrmOcclusionMask : AmbiMonoBehaviour<WyrmOcclusionMask>
{
    [SerializeField]
    OcclusionMaskType _type = OcclusionMaskType.Singular;

    [Range(1, HC.MAX_OCC_SAMPLES_PER_MASK)]
    [SerializeField]
    int _sampleCount = 1;

    [Range(0.2f, 10f)]
    [SerializeField]
    float _size = 1f;

    float Radius => _size / 2;

    readonly List<Vector3> _generatedPositionsBuffer = new(HC.MAX_OCC_SAMPLES_PER_MASK);

    [HideInInspector]
    public List<MaskSampleData> _generatedSampleData = new(HC.MAX_OCC_SAMPLES_PER_MASK);

    void OnEnable()
    {
        if (_generatedSampleData.Count == 0)
            RebuildSampleBuffer();

        Register();
    }

    void OnDisable()
    {
        Deregister();
    }

    void OnValidate()
    {
        RebuildSampleBuffer();

        if (IsRegistered)
            SyncAllSamplesToNative();
    }

    public void UpdateSampleLocalPosition(int sampleIndex, Vector3 newLocalPosition)
    {
        if (sampleIndex < 0 || sampleIndex >= _generatedSampleData.Count)
            return;

        var sample = _generatedSampleData[sampleIndex];
        sample.LocalPosition = newLocalPosition;
        _generatedSampleData[sampleIndex] = sample;

        if (IsRegistered)
        {
            int SoASampleIndex = (SoAIndex * HC.MAX_OCC_SAMPLES_PER_MASK) + sampleIndex;
            SampleLocalPositions[SoASampleIndex] = newLocalPosition;
        }
    }

    public void SyncAllSamplesToNative()
    {
        if (!IsRegistered)
            return;

        MaskSampleCounts[SoAIndex] = _generatedSampleData.Count;
        int chunkStartOffset = SoAIndex * HC.MAX_OCC_SAMPLES_PER_MASK;

        for (int i = 0; i < _generatedSampleData.Count; i++)
        {
            SampleLocalPositions[chunkStartOffset + i] = _generatedSampleData[i].LocalPosition;
            SampleWeights[chunkStartOffset + i] = _generatedSampleData[i].Weight;
            SampleIsDiscardable[chunkStartOffset + i] = _generatedSampleData[i].Discardable;
        }
    }

    void RebuildSampleBuffer()
    {
        _generatedSampleData.Clear();
        _generatedPositionsBuffer.Clear();

        if (_type is OcclusionMaskType.SphericalHalton or OcclusionMaskType.Singular)
        {
            if (_type is OcclusionMaskType.SphericalHalton)
                HaltonSequence.GenerateSphereVolumeSamples(Radius, _sampleCount, _generatedPositionsBuffer);
            else
                _generatedPositionsBuffer.Add(Vector3.zero);

            for (var i = 0; i < _generatedPositionsBuffer.Count; i++)
            {
                _generatedSampleData.Add(
                    new MaskSampleData
                    {
                        LocalPosition = _generatedPositionsBuffer[i],
                        Weight = 1f,
                        Discardable = true,
                    }
                );
            }
        }
    }
}

enum OcclusionMaskType : byte
{
    Singular,
    SphericalHalton,
    Custom,
}
