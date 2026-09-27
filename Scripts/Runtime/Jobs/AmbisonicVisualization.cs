#if UNITY_EDITOR
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

// Field: xyz = low, mid, high coefficient sums per channel.
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
    public NativeReference<int> FieldOrder;

    public void Execute()
    {
        float4* field = (float4*)Field.GetUnsafePtr();
        UnsafeUtility.MemClear(field, HC.MAX_AMBISONIC_CHANNELS * sizeof(float4));

        float* outputs = (float*)SourceOutputs.GetUnsafeReadOnlyPtr();
        float* maxREWeights = (float*)MaxREWeights.GetUnsafeReadOnlyPtr();
        int fieldOrder = 0;

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
                fieldOrder = math.max(fieldOrder, order);

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

        FieldOrder.Value = fieldOrder;
    }
}
#endif
