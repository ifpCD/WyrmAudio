using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

// Owns the layout of banded moment blocks: PolygonProjection.BANDED_MOMENTS double4 per block, laid out [band][moment],
// one block per mesh chunk in a flat buffer. The layout inside a band ([order][direction group]) belongs to PolygonProjection.
internal static unsafe class BandedMoments
{
    // capacity in blocks; the list grows through Resize as chunks are rebuilt
    public static NativeList<double4> Allocate(int capacity, Allocator allocator) => new(capacity * PolygonProjection.BANDED_MOMENTS, allocator);

    public static void Resize(NativeList<double4> blocks, int count) => blocks.ResizeUninitialized(count * PolygonProjection.BANDED_MOMENTS);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double4* Get(in NativeArray<double4> blocks, int block) =>
        (double4*)blocks.GetUnsafePtr() + block * PolygonProjection.BANDED_MOMENTS;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double4* GetReadOnly(in NativeArray<double4> blocks, int block) =>
        (double4*)blocks.GetUnsafeReadOnlyPtr() + block * PolygonProjection.BANDED_MOMENTS;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double4* GetBand(double4* moments, int band) => moments + band * PolygonProjection.MOMENTS;

    public static void Clear(double4* moments) => UnsafeUtility.MemClear(moments, PolygonProjection.BANDED_MOMENTS * sizeof(double4));
}
