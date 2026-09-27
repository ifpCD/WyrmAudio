#if UNITY_EDITOR
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

// Field: xyz = low, mid, high coefficient sums per channel. FieldOrders: highest order present per band.
[BurstCompile(CompileSynchronously = true)]
internal unsafe struct AccumulateAmbisonicFieldJob : IJob
{
    public int SourceCount;
    public bool Rendered;
    public float3 BandVisibility;

    [ReadOnly]
    public NativeArray<float> SourceWeights;

    [ReadOnly]
    public NativeArray<float> SourceOutputs;

    [ReadOnly]
    public NativeArray<int3> SourceBandOrders;

    [ReadOnly]
    public NativeArray<float> MaxREWeights;

    public NativeArray<float4> Field;
    public NativeReference<int3> FieldOrders;

    public void Execute()
    {
        float4* field = (float4*)Field.GetUnsafePtr();
        UnsafeUtility.MemClear(field, HC.MAX_AMBISONIC_CHANNELS * sizeof(float4));

        float* outputs = (float*)SourceOutputs.GetUnsafeReadOnlyPtr();
        float* maxREWeights = (float*)MaxREWeights.GetUnsafeReadOnlyPtr();
        int3 fieldOrders = int3.zero;

        for (int source = 0; source < SourceCount; source++)
        {
            float sourceWeight = SourceWeights[source];

            if (sourceWeight == 0f)
                continue;

            int3 orders = SourceBandOrders[source];

            for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
            {
                float bandWeight = sourceWeight * BandVisibility[band];

                if (bandWeight == 0f)
                    continue;

                int order = orders[band];
                fieldOrders[band] = math.max(fieldOrders[band], order);

                float* coefficients = outputs + source * HC.AMBISONIC_BUFFER_LENGTH + band * HC.MAX_AMBISONIC_CHANNELS;
                float* degreeWeights = maxREWeights + order * (HC.MAX_AMBISONIC_ORDER + 1);

                for (int l = 0, channel = 0; l <= order; l++)
                {
                    float weight = Rendered ? bandWeight * degreeWeights[l] : bandWeight;

                    for (int m = -l; m <= l; m++, channel++)
                        field[channel][band] += coefficients[channel] * weight;
                }
            }
        }

        FieldOrders.Value = fieldOrders;
    }
}

// Phonon's sampling decoder at its head-locked virtual speakers: feed_k = 4pi/K * field(R s_k), exactly the weight each
// speaker's HRTF receives in the order-N projection. Energy vectors: sum(feed^2 * s) / sum(feed^2) per band and broadband;
// on the 21-design they are exact for every order up to 10.
[BurstCompile(CompileSynchronously = true)]
internal unsafe struct DecodeVirtualSpeakersJob : IJob
{
    public int Order;
    public quaternion HeadRotation;

    [ReadOnly]
    public NativeArray<float3> SpeakerDirections;

    [ReadOnly]
    public NativeArray<float4> Field;

    [WriteOnly]
    public NativeArray<float4> SpeakerFeeds;

    [WriteOnly]
    public NativeArray<float4> EnergyVectors;

    public void Execute()
    {
        float* basis = stackalloc float[HC.MAX_AMBISONIC_CHANNELS];
        float4* field = (float4*)Field.GetUnsafeReadOnlyPtr();
        int channels = SphericalHarmonics.ChannelCount(Order);
        float quadrature = 4f * math.PI / VirtualSpeakerLayout.COUNT;

        float3 lowVector = float3.zero;
        float3 midVector = float3.zero;
        float3 highVector = float3.zero;
        float3 broadbandVector = float3.zero;
        float4 energies = float4.zero;

        for (int speaker = 0; speaker < VirtualSpeakerLayout.COUNT; speaker++)
        {
            float3 direction = math.rotate(HeadRotation, SpeakerDirections[speaker]);
            SphericalHarmonics.Evaluate(direction, Order, basis);

            float4 feed = float4.zero;

            for (int channel = 0; channel < channels; channel++)
                feed += basis[channel] * field[channel];

            feed *= quadrature;
            SpeakerFeeds[speaker] = feed;

            float3 energy = feed.xyz * feed.xyz;
            float broadband = math.csum(energy);

            energies += new float4(energy, broadband);
            lowVector += energy.x * direction;
            midVector += energy.y * direction;
            highVector += energy.z * direction;
            broadbandVector += broadband * direction;
        }

        EnergyVectors[0] = EnergyVector(lowVector, energies.x);
        EnergyVectors[1] = EnergyVector(midVector, energies.y);
        EnergyVectors[2] = EnergyVector(highVector, energies.z);
        EnergyVectors[3] = EnergyVector(broadbandVector, energies.w);
    }

    static float4 EnergyVector(float3 weighted, float energy) => energy > 0f ? new float4(weighted / energy, energy) : float4.zero;
}
#endif
