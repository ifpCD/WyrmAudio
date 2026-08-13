using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[RequireComponent(typeof(AudioListener))]
public class WyrmListener : AmbiMonoBehaviour<WyrmListener>
{
    protected override int AllocatedCapacity => 1;

    public static NativeReference<int> ListenerRoomIdentifier;
    public static NativeReference<float3> ListenerPosition;
    public static NativeReference<quaternion> ListenerRotation;

    void OnEnable() => Register();

    void OnDisable() => Deregister();

    protected override void AllocateNative()
    {
        ListenerRoomIdentifier = new(allocator: Allocator.Persistent);
        ListenerPosition = new(allocator: Allocator.Persistent);
        ListenerRotation = new(allocator: Allocator.Persistent);
    }

    protected override void DeallocateNative()
    {
        ListenerRoomIdentifier.TryDispose();
        ListenerPosition.TryDispose();
        ListenerRotation.TryDispose();
    }

    internal static void Synchronize()
    {
        if (CompletelyInactive)
            return;

        Transform listenerTransform = RegisteredInstances[0].transform;
        ListenerPosition.Value = listenerTransform.position;
        ListenerRotation.Value = listenerTransform.rotation;
    }

    protected override void LoadObjectToArrays()
    {
        ListenerRoomIdentifier.Value = -1;
        Synchronize();
    }

    protected override void RemoveAtSwapBack(int removedIndex, int lastIndex) { }
}
