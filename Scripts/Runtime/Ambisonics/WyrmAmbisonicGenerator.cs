using Unity.Collections;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[NoAutoStaticsCleanup]
public class WyrmAmbisonicGenerator : AmbiMonoBehaviour<WyrmAmbisonicGenerator>
{
    protected override int AllocatedCapacity => HC.MAX_AMBISONIC_CONTRIBUTORS;

    AmbisonicGeneratorType _type;

    public AmbisonicGeneratorType Type
    {
        get => _type;
        set
        {
            _type = value;

            if (IsRegistered)
                Types[SoAIndex] = (byte)value;
        }
    }

    float _volume;
    float _volumeLow;
    float _volumeMid;
    float _volumeHigh;

    public float Volume
    {
        get => _volume;
        set
        {
            value = Mathf.Clamp01(value);
            _volume = value;

            if (IsRegistered)
                Volume01s[SoAIndex] = value;
        }
    }

    public float VolumeLow
    {
        get => _volumeLow;
        set
        {
            value = Mathf.Clamp01(value);
            _volumeLow = value;

            if (!IsRegistered)
                return;

            var eq = EQVolume01s[SoAIndex];
            eq.x = value;
            EQVolume01s[SoAIndex] = eq;
        }
    }

    public float VolumeMid
    {
        get => _volumeMid;
        set
        {
            value = Mathf.Clamp01(value);
            _volumeMid = value;

            if (!IsRegistered)
                return;

            var eq = EQVolume01s[SoAIndex];
            eq.y = value;
            EQVolume01s[SoAIndex] = eq;
        }
    }

    public float VolumeHigh
    {
        get => _volumeHigh;
        set
        {
            value = Mathf.Clamp01(value);
            _volumeHigh = value;

            if (!IsRegistered)
                return;

            var eq = EQVolume01s[SoAIndex];
            eq.z = value;
            EQVolume01s[SoAIndex] = eq;
        }
    }

    public int TargetSourceIndex;

    public static NativeArray<byte> Types;

    public static NativeArray<float> Volume01s;
    public static NativeArray<float3> EQVolume01s;

    public static NativeArray<float> CurrentAmbisonicOutputs;
    public static NativeArray<float> TargetAmbisonicOutputs;

    public static NativeArray<int> TargetSourceIndices;

    // csharpier-ignore
    protected override void AllocateNative()
    {
        Types                   = new(AllocatedCapacity, Allocator.Persistent);

        Volume01s                 = new(AllocatedCapacity, Allocator.Persistent);
        EQVolume01s             = new(AllocatedCapacity, Allocator.Persistent);

        CurrentAmbisonicOutputs = new(AllocatedCapacity * 48, Allocator.Persistent);
        TargetAmbisonicOutputs  = new(AllocatedCapacity * 48, Allocator.Persistent);

        TargetSourceIndices     = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        Types.TryDispose();

        Volume01s.TryDispose();
        EQVolume01s.TryDispose();

        CurrentAmbisonicOutputs.TryDispose();
        TargetAmbisonicOutputs.TryDispose();
    }

    // csharpier-ignore
    protected override void LoadObjectToArrays()
    {
        Types[SoAIndex]       = (byte)Type;

        Volume01s[SoAIndex]     = Volume;
        EQVolume01s[SoAIndex] = new float3
        {
            x = VolumeLow,
            y = VolumeMid,
            z = VolumeHigh,
        };
    }

    // csharpier-ignore
    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            Types[removedIndex]                   = Types[lastIndex];

            Volume01s[removedIndex]                 = Volume01s[lastIndex];
            EQVolume01s[removedIndex]             = EQVolume01s[lastIndex];

            CurrentAmbisonicOutputs[removedIndex] = CurrentAmbisonicOutputs[lastIndex];
            TargetAmbisonicOutputs[removedIndex]  = TargetAmbisonicOutputs[lastIndex];
        }
    }
}
