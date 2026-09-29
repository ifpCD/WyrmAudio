using Unity.Collections;
using Unity.Mathematics;

public sealed partial class WyrmAmbisonicGenerator
{
    internal static NativeArray<int3> BandOrders;
    internal static NativeArray<float3> BandGains;
    internal static NativeArray<float3> BandSpreads;
    internal static NativeArray<float> HorizontalSpreads;

    internal static NativeArray<float> Outputs;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        BandOrders        = new(AllocatedCapacity, Allocator.Persistent);
        BandGains         = new(AllocatedCapacity, Allocator.Persistent);
        BandSpreads       = new(AllocatedCapacity, Allocator.Persistent);
        HorizontalSpreads = new(AllocatedCapacity, Allocator.Persistent);
        Outputs           = AmbisonicBuffer.Allocate(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        BandOrders.TryDispose();
        BandGains.TryDispose();
        BandSpreads.TryDispose();
        HorizontalSpreads.TryDispose();
        Outputs.TryDispose();
    }

    protected override void LoadObjectToArrays()
    {
        AmbisonicBuffer.Clear(Outputs, SoAIndex);

        SyncShaping();
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex == lastIndex)
            return;

        BandOrders[removedIndex]        = BandOrders[lastIndex];
        BandGains[removedIndex]         = BandGains[lastIndex];
        BandSpreads[removedIndex]       = BandSpreads[lastIndex];
        HorizontalSpreads[removedIndex] = HorizontalSpreads[lastIndex];

        AmbisonicBuffer.Copy(Outputs, lastIndex, Outputs, removedIndex);
    }

    // csharpier-ignore
    void SyncShaping()
    {
        if (!IsRegistered)
            return;

        BandOrders[SoAIndex]        = ClampedOrders(_ambisonicOrders);
        BandGains[SoAIndex]         = BandGain;
        BandSpreads[SoAIndex]       = BandSpreadRadians;
        HorizontalSpreads[SoAIndex] = math.radians(_horizontalSpreadDegrees);
    }
}
