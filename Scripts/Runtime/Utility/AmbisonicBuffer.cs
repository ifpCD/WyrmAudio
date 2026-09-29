using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// Owns the layout of the flat banded ambisonic buffers (generator outputs, source outputs): one slot of
// HC.AMBISONIC_BUFFER_LENGTH floats per generator or source, laid out [band][ACN channel]. Nothing else multiplies by these
// strides; callers address slots and bands here, and the math on the coefficients stays in the jobs.
internal static unsafe class AmbisonicBuffer
{
    public static NativeArray<float> Allocate(int slots, Allocator allocator) => new(slots * HC.AMBISONIC_BUFFER_LENGTH, allocator);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float* Get(in NativeArray<float> buffers, int slot) => (float*)buffers.GetUnsafePtr() + slot * HC.AMBISONIC_BUFFER_LENGTH;

    // for [ReadOnly] job fields: Get requires write access under the safety checks
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float* GetReadOnly(in NativeArray<float> buffers, int slot) =>
        (float*)buffers.GetUnsafeReadOnlyPtr() + slot * HC.AMBISONIC_BUFFER_LENGTH;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float* GetBand(float* buffer, int band) => buffer + band * HC.MAX_AMBISONIC_CHANNELS;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(float* buffer) => UnsafeUtility.MemClear(buffer, HC.AMBISONIC_BUFFER_LENGTH * sizeof(float));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearBand(float* band) => UnsafeUtility.MemClear(band, HC.MAX_AMBISONIC_CHANNELS * sizeof(float));

    // count consecutive slots, starting at slot
    public static void Clear(in NativeArray<float> buffers, int slot, int count = 1)
    {
        float* ptr = Get(buffers, slot);
        UnsafeUtility.MemClear(ptr, (long)count * HC.AMBISONIC_BUFFER_LENGTH * sizeof(float));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy(in NativeArray<float> src, int srcSlot, in NativeArray<float> dst, int dstSlot)
    {
        NativeArray<float>.Copy(src, srcSlot * HC.AMBISONIC_BUFFER_LENGTH, dst, dstSlot * HC.AMBISONIC_BUFFER_LENGTH, HC.AMBISONIC_BUFFER_LENGTH);
    }
}
