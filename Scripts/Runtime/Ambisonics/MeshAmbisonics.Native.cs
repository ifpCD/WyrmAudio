using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

internal sealed partial class MeshAmbisonics
{
    internal static TransformAccessArray Targets;
    internal static NativeArray<AmbiHandle> GeneratorHandles;
    internal static NativeArray<float4x4> LocalToAmbisonic;

    internal static NativeArray<double4> MomentDirectionsX;
    internal static NativeArray<double4> MomentDirectionsY;
    internal static NativeArray<double4> MomentDirectionsZ;
    internal static NativeArray<double4> MomentToHarmonic;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        Targets           = new(AllocatedCapacity);
        GeneratorHandles  = new(AllocatedCapacity, Allocator.Persistent);
        LocalToAmbisonic  = new(AllocatedCapacity, Allocator.Persistent);

        MomentDirectionsX = new(PolygonProjection.GROUPS, Allocator.Persistent);
        MomentDirectionsY = new(PolygonProjection.GROUPS, Allocator.Persistent);
        MomentDirectionsZ = new(PolygonProjection.GROUPS, Allocator.Persistent);
        MomentToHarmonic  = new(HC.MAX_AMBISONIC_CHANNELS * PolygonProjection.MOMENTS, Allocator.Persistent);

        PolygonProjection.BuildTables(MomentDirectionsX, MomentDirectionsY, MomentDirectionsZ, MomentToHarmonic);
        AllocateGeometry(AllocatedCapacity);
    }

    protected override void DeallocateNative()
    {
        Targets.TryDispose();
        GeneratorHandles.TryDispose();
        LocalToAmbisonic.TryDispose();

        MomentDirectionsX.TryDispose();
        MomentDirectionsY.TryDispose();
        MomentDirectionsZ.TryDispose();
        MomentToHarmonic.TryDispose();

        DeallocateGeometry();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        Projection.Complete();
        Targets.Add(_generator.MeshTarget.transform);
        GeneratorHandles[SoAIndex] = _generator.Handle;
        CacheTriangles(_generator.MeshTarget.sharedMesh, _generator.MeshVertexColorBands);
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        Projection.Complete();
        RemoveTriangles(removedIndex, lastIndex);
        Targets.RemoveAtSwapBack(removedIndex);
        GeneratorHandles[removedIndex] = GeneratorHandles[lastIndex];
    }
}
