using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

internal static class AmbisonicUtilities
{
    public static void Copy(in NativeArray<float> src, int srcIndex, in NativeArray<float> dst, int dstIndex)
    {
        NativeArray<float>.Copy(src, srcIndex * HC.AMBISONIC_BUFFER_LENGTH, dst, dstIndex * HC.AMBISONIC_BUFFER_LENGTH, HC.AMBISONIC_BUFFER_LENGTH);
    }

    public static unsafe void Clear(in NativeArray<float> buffer, int index)
    {
        int offset = index * HC.AMBISONIC_BUFFER_LENGTH;
        float* target = (float*)buffer.GetUnsafePtr() + offset;

        UnsafeUtility.MemClear(target, HC.AMBISONIC_BUFFER_LENGTH * sizeof(float));
    }
}
