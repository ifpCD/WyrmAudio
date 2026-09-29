using System.Runtime.CompilerServices;
using Unity.Collections;

// Owns the layout of the per-sample occlusion arrays: every mask gets a fixed slot of HC.MAX_OCC_SAMPLES_PER_MASK entries,
// of which the first MaskSampleCounts[mask] are live. The raycast buffers are packed densely by MaskRaycastOffsets instead.
internal static class OcclusionSamples
{
    // room for every mask filling its whole slot
    public static NativeArray<T> Allocate<T>(int maskCount, Allocator allocator)
        where T : struct
    {
        return new NativeArray<T>(maskCount * HC.MAX_OCC_SAMPLES_PER_MASK, allocator);
    }

    // index of the mask's first sample
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Start(int maskIndex) => maskIndex * HC.MAX_OCC_SAMPLES_PER_MASK;

    public static void Copy<T>(in NativeArray<T> src, int srcIndex, in NativeArray<T> dst, int dstIndex)
        where T : struct
    {
        NativeArray<T>.Copy(src, Start(srcIndex), dst, Start(dstIndex), HC.MAX_OCC_SAMPLES_PER_MASK);
    }
}
