using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmMixerGroupProcessor : MonoBehaviour
{
    [BurstCompile]
    private struct GatherTrackedPositionsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<byte> IsTracking;
        [WriteOnly] public NativeArray<float3> TrackedPositions;

        public void Execute(int index, TransformAccess transform)
        {
            if (IsTracking[index] == 1)
            {
                TrackedPositions[index] = transform.position;
            }
        }
    }

    [BurstCompile]
    private struct ApplySourceTransformsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<byte> IsTracking;
        [ReadOnly] public NativeArray<float3> TrackedPositions;
        [WriteOnly] public NativeArray<float3> SourcePositions;

        public void Execute(int index, TransformAccess transform)
        {
            if (IsTracking[index] == 1)
            {
                float3 targetPos = TrackedPositions[index];
                transform.position = targetPos;
                SourcePositions[index] = targetPos;
            }
        }
    }
}