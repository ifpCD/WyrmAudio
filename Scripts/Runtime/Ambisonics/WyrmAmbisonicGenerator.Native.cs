using Unity.Collections;
using Unity.Mathematics;

public sealed partial class WyrmAmbisonicGenerator
{
    internal static NativeArray<float3> BandGains;
    internal static NativeArray<float3> BandSpreads;
    internal static NativeArray<float> HorizontalSpreads;

    // rewritten every scheduled frame; Phonon interpolates per audio block, so no smoothing (and no latency) here
    internal static NativeArray<float> Outputs;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        BandGains         = new(AllocatedCapacity, Allocator.Persistent);
        BandSpreads       = new(AllocatedCapacity, Allocator.Persistent);
        HorizontalSpreads = new(AllocatedCapacity, Allocator.Persistent);
        Outputs           = new(AllocatedCapacity * HC.AMBISONIC_BUFFER_LENGTH, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        BandGains.TryDispose();
        BandSpreads.TryDispose();
        HorizontalSpreads.TryDispose();
        Outputs.TryDispose();
    }

    protected override void LoadObjectToArrays()
    {
        int offset = SoAIndex * HC.AMBISONIC_BUFFER_LENGTH;

        for (int index = offset; index < offset + HC.AMBISONIC_BUFFER_LENGTH; index++)
            Outputs[index] = 0f;

        SyncShaping();
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex == lastIndex)
            return;

        BandGains[removedIndex]         = BandGains[lastIndex];
        BandSpreads[removedIndex]       = BandSpreads[lastIndex];
        HorizontalSpreads[removedIndex] = HorizontalSpreads[lastIndex];

        NativeArray<float>.Copy(Outputs, lastIndex * HC.AMBISONIC_BUFFER_LENGTH, Outputs, removedIndex * HC.AMBISONIC_BUFFER_LENGTH, HC.AMBISONIC_BUFFER_LENGTH);
    }

    // csharpier-ignore
    void SyncShaping()
    {
        if (!IsRegistered)
            return;

        BandGains[SoAIndex]         = BandGain;
        BandSpreads[SoAIndex]       = BandSpreadRadians;
        HorizontalSpreads[SoAIndex] = math.radians(_horizontalSpreadDegrees);
    }
}
