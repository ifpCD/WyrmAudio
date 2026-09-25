using Unity.Collections;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[NoAutoStaticsCleanup]
public class WyrmAmbisonicContributor : AmbiBase<WyrmAmbisonicContributor>
{
    protected override int AllocatedCapacity => throw new System.NotImplementedException();

    public AmbisonicGeneratorType _Type;

    public AmbisonicGeneratorType Type
    {
        get => _Type;
        set
        {
            _Type = value;

            if (IsRegistered)
                Types[SoAIndex] = (byte)value;
        }
    }

    public float _Volume;
    public float _VolumeLow;
    public float _VolumeMid;
    public float _VolumeHigh;

    public float Volume
    {
        get => _Volume;
        set
        {
            value = Mathf.Clamp01(value);
            _Volume = value;

            if (IsRegistered)
                Volumes[SoAIndex] = value;
        }
    }

    public float VolumeLow
    {
        get => _VolumeLow;
        set
        {
            value = Mathf.Clamp01(value);
            _VolumeLow = value;

            if (!IsRegistered)
                return;

            var eq = EQVolume01s[SoAIndex];
            eq.x = value;
            EQVolume01s[SoAIndex] = eq;
        }
    }

    public float VolumeMid
    {
        get => _VolumeMid;
        set
        {
            value = Mathf.Clamp01(value);
            _VolumeMid = value;

            if (!IsRegistered)
                return;

            var eq = EQVolume01s[SoAIndex];
            eq.y = value;
            EQVolume01s[SoAIndex] = eq;
        }
    }

    public float VolumeHigh
    {
        get => _VolumeHigh;
        set
        {
            value = Mathf.Clamp01(value);
            _VolumeHigh = value;

            if (!IsRegistered)
                return;

            var eq = EQVolume01s[SoAIndex];
            eq.z = value;
            EQVolume01s[SoAIndex] = eq;
        }
    }

    public int TargetSourceIndex;

    public static NativeArray<byte> Types;

    public static NativeArray<float> Volumes;
    public static NativeArray<float3> EQVolume01s;

    public static NativeArray<float> CurrentAmbisonicOutputs;
    public static NativeArray<float> TargetAmbisonicOutputs;

    public static NativeArray<int> TargetSourceIndices;

    protected override void AllocateNative()
    {
        Types = new(AllocatedCapacity, Allocator.Persistent);

        Volumes = new(AllocatedCapacity, Allocator.Persistent);
        EQVolume01s = new(AllocatedCapacity, Allocator.Persistent);

        CurrentAmbisonicOutputs = new(AllocatedCapacity * 48, Allocator.Persistent);
        TargetAmbisonicOutputs = new(AllocatedCapacity * 48, Allocator.Persistent);

        TargetSourceIndices = new(AllocatedCapacity, Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        Types.TryDispose();

        Volumes.TryDispose();
        EQVolume01s.TryDispose();

        CurrentAmbisonicOutputs.TryDispose();
        TargetAmbisonicOutputs.TryDispose();

        TargetSourceIndices.TryDispose();
    }

    protected override void LoadObjectToArrays()
    {
        Types[SoAIndex] = (byte)Type;

        Volumes[SoAIndex] = Volume;
        EQVolume01s[SoAIndex] = new float3
        {
            x = VolumeLow,
            y = VolumeMid,
            z = VolumeHigh,
        };

        TargetSourceIndices[SoAIndex] = TargetSourceIndex;
    }

    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex)
    {
        if (removedIndex != lastIndex)
        {
            Types[removedIndex] = Types[lastIndex];

            Volumes[removedIndex] = Volumes[lastIndex];
            EQVolume01s[removedIndex] = EQVolume01s[lastIndex];

            CurrentAmbisonicOutputs[removedIndex] = CurrentAmbisonicOutputs[lastIndex];
            TargetAmbisonicOutputs[removedIndex] = TargetAmbisonicOutputs[lastIndex];

            TargetSourceIndices[removedIndex] = TargetSourceIndices[lastIndex];
        }
    }
}
