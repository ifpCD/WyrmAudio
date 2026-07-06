using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct WyrmRoomVolumeMixJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<byte> SourceActiveStates;

    [ReadOnly] public NativeArray<float3> SourcePositions;

    [ReadOnly] public float3 ListenerPosition;

    [ReadOnly] public NativeArray<float> SourceMinDistances;

    [ReadOnly] public NativeArray<float> SourceMaxDistances;

    [WriteOnly] public NativeArray<float> OutputNormalizedRoomMixVolume;

    // if listener is within min distance = 1
    // if listener is between min and max distances = variable
    // if listener is outside max distance = 0
    public void Execute(int index)
    {
        // this value is used inside OnAudioFilterRead for fading
        OutputNormalizedRoomMixVolume[index] = 0f;
    }
}