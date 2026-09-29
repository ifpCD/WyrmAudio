using System.Runtime.CompilerServices;
using Unity.Collections;

// Owns the layout of the per-sample occlusion arrays: every mask gets a fixed slot of HC.MAX_OCC_SAMPLES_PER_MASK entries,
// of which the first MaskSampleCounts[mask] are live. The raycast buffers are packed densely by MaskRaycastOffsets instead.
internal static class OcclusionSamples
{
    // room for every mask filling its whole slot
    public static NativeArray<T> Allocate<T>(int masks, Allocator allocator)
        where T : struct
    {
        return new NativeArray<T>(masks * HC.MAX_OCC_SAMPLES_PER_MASK, allocator);
    }

    // index of the mask's first sample
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Start(int mask) => mask * HC.MAX_OCC_SAMPLES_PER_MASK;

    public static void Copy<T>(in NativeArray<T> src, int srcMask, in NativeArray<T> dst, int dstMask)
        where T : struct
    {
        NativeArray<T>.Copy(src, Start(srcMask), dst, Start(dstMask), HC.MAX_OCC_SAMPLES_PER_MASK);
    }
}
