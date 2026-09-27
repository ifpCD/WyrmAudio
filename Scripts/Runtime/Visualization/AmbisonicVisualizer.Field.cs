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
    NativeReference<int3> _fieldOrders;
    int _contributingSources;

    readonly Vector4[] _fieldUpload = new Vector4[HC.MAX_AMBISONIC_CHANNELS];

    // SphericalHarmonics.Evaluate's constants for the shader, at index l(l+1)+m with 0 <= m <= l:
    // xy = (a, b) of the degree recurrence for l > m, z = sectoral factor for l == m > 0
    static Vector4[] BuildRecurrence()
    {
        var recurrence = new Vector4[HC.MAX_AMBISONIC_CHANNELS];

        for (int m = 0; m <= HC.MAX_AMBISONIC_ORDER; m++)
        {
            recurrence[m * (m + 2)].z = m == 0 ? 1f : m == 1 ? math.sqrt(3f) : math.sqrt((2f * m + 1f) / (2f * m));

            for (int l = m + 1; l <= HC.MAX_AMBISONIC_ORDER; l++)
            {
                float l2 = l * l;
                float m2 = m * m;

                recurrence[l * (l + 1) + m] =
                    l == m + 1
                        ? new Vector4(math.sqrt(2f * m + 3f), 0f, 0f, 0f)
                        : new Vector4(
                            math.sqrt((4f * l2 - 1f) / (l2 - m2)),
                            math.sqrt(((l - 1f) * (l - 1f) - m2) * (2f * l + 1f) / ((2f * l - 3f) * (l2 - m2))),
                            0f,
                            0f
                        );
            }
        }

        return recurrence;
    }

    void AllocateField()
    {
        _field = new(HC.MAX_AMBISONIC_CHANNELS, Allocator.Persistent);
        _fieldOrders = new(Allocator.Persistent);
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
        _fieldOrders.TryDispose();
    }

    // returns the highest band order present
    int AccumulateField()
    {
        int sourceCount = WyrmBaseSource.ActiveCount;

        if (!_sourceWeights.IsCreated || _sourceWeights.Length < sourceCount)
        {
            _sourceWeights.TryDispose();
            _sourceWeights = new(math.ceilpow2(sourceCount), Allocator.Persistent);
        }

        _contributingSources = 0;

        for (int index = 0; index < sourceCount; index++)
        {
            WyrmBaseSource source = WyrmBaseSource.RegisteredInstances[index];
            bool included = source.UseAmbisonics && (targetMixerGroup == null || source.MixerGroup == targetMixerGroup);
            float weight = included ? (weightByVolume ? source.volume : 1f) : 0f;

            _sourceWeights[index] = weight;

            if (weight > 0f)
                _contributingSources++;
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
            FieldOrders = _fieldOrders,
        }.Run();

        for (int channel = 0; channel < HC.MAX_AMBISONIC_CHANNELS; channel++)
            _fieldUpload[channel] = _field[channel];

        return math.cmax(_fieldOrders.Value);
    }
}
#endif
