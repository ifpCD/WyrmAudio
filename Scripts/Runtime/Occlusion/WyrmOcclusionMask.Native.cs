using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public partial class WyrmOcclusionMask
{
    protected override int AllocatedCapacity => 8000;

    public static NativeArray<float> TotalSampleWeight;
    public static NativeArray<float> OccludedSampleWeight;

    public static NativeArray<float> TargetOcclusionValue01s;
    public static NativeArray<float4x4> LocalToWorlds;
    public static TransformAccessArray MaskTransforms;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        TotalSampleWeight       = new(AllocatedCapacity, Allocator.Persistent);
        OccludedSampleWeight    = new(AllocatedCapacity, Allocator.Persistent);

        TargetOcclusionValue01s = new(AllocatedCapacity, Allocator.Persistent);
        LocalToWorlds           = new(AllocatedCapacity, Allocator.Persistent);
        MaskTransforms          = new(AllocatedCapacity);
    }

    protected override void DeallocateNative()
    {
        TotalSampleWeight.TryDispose();
        OccludedSampleWeight.TryDispose();
        
        TargetOcclusionValue01s.TryDispose();
        LocalToWorlds.TryDispose();
        MaskTransforms.TryDispose();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        TargetOcclusionValue01s[SoAIndex] = 0f;
        LocalToWorlds[SoAIndex]           = transform.localToWorldMatrix;
        MaskTransforms.Add(transform);
    }

    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            TargetOcclusionValue01s[removedIndex] = TargetOcclusionValue01s[lastIndex];
            LocalToWorlds[removedIndex] = LocalToWorlds[lastIndex];

            var movedMask = RegisteredInstances[removedIndex];
            if (movedMask != null && movedMask._sampleBuffer != null)
            {
                foreach (var sample in movedMask._sampleBuffer)
                {
                    if (sample.IsRegistered)
                        WyrmOcclusionSample.SampleToMask[sample.SoAIndex] = removedIndex;
                }
            }

            for (int i = 0; i < WyrmBaseSource.ActiveCount; i++)
            {
                if (WyrmBaseSource.OcclusionMaskIndex[i] == lastIndex)
                    WyrmBaseSource.OcclusionMaskIndex[i] = removedIndex;
                else if (WyrmBaseSource.OcclusionMaskIndex[i] == removedIndex)
                    WyrmBaseSource.OcclusionMaskIndex[i] = -1;
            }
        }

        MaskTransforms.RemoveAtSwapBack(removedIndex);
    }
}
