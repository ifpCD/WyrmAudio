#if UNITY_EDITOR
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed partial class AmbisonicVisualizer
{
    const int WEIGHT_STRIDE = HC.MAX_AMBISONIC_ORDER + 1;

    NativeArray<float> _sourceWeights;
    NativeArray<float> _maxREWeights;
    NativeArray<float4> _field;
    NativeReference<int> _fieldOrder;

    readonly Vector4[] _fieldUpload = new Vector4[HC.MAX_AMBISONIC_CHANNELS];

    void AllocateField()
    {
        _field = new(HC.MAX_AMBISONIC_CHANNELS, Allocator.Persistent);
        _fieldOrder = new(Allocator.Persistent);
        _maxREWeights = new(WEIGHT_STRIDE * WEIGHT_STRIDE, Allocator.Persistent);

        // Phonon's decoder weighting: P_l(cos(137.9 deg / (order + 1.51)))
        for (int order = 0; order <= HC.MAX_AMBISONIC_ORDER; order++)
        {
            double cosine = math.cos(math.radians(137.9) / (order + 1.51));
            double previous = 1.0;
            double current = cosine;

            for (int l = 0; l <= order; l++)
            {
                if (l > 1)
                {
                    double next = ((2 * l - 1) * cosine * current - (l - 1) * previous) / l;
                    previous = current;
                    current = next;
                }

                _maxREWeights[order * WEIGHT_STRIDE + l] = (float)(l == 0 ? 1.0 : l == 1 ? cosine : current);
            }
        }
    }

    void DeallocateField()
    {
        _sourceWeights.TryDispose();
        _maxREWeights.TryDispose();
        _field.TryDispose();
        _fieldOrder.TryDispose();
    }

    int AccumulateField()
    {
        int sourceCount = WyrmBaseSource.ActiveCount;

        if (!_sourceWeights.IsCreated || _sourceWeights.Length < sourceCount)
        {
            _sourceWeights.TryDispose();
            _sourceWeights = new(math.ceilpow2(sourceCount), Allocator.Persistent);
        }

        for (int index = 0; index < sourceCount; index++)
        {
            WyrmBaseSource source = WyrmBaseSource.RegisteredInstances[index];
            bool included = source.UseAmbisonics && (targetMixerGroup == null || source.MixerGroup == targetMixerGroup);
            _sourceWeights[index] = included ? (weightByVolume ? source.volume : 1f) : 0f;
        }

        new AccumulateAmbisonicFieldJob
        {
            SourceCount = sourceCount,
            Rendered = visualization == AmbisonicVisualization.Rendered,
            BandVisibility = bandVisibility,
            SourceWeights = _sourceWeights,
            SourceOutputs = WyrmBaseSource.TargetAmbisonicOutputs,
            SourceBandOrders = WyrmBaseSource.TargetAmbisonicOrders,
            MaxREWeights = _maxREWeights,
            Field = _field,
            FieldOrder = _fieldOrder,
        }.Run();

        for (int channel = 0; channel < HC.MAX_AMBISONIC_CHANNELS; channel++)
            _fieldUpload[channel] = _field[channel];

        return _fieldOrder.Value;
    }
}
#endif
