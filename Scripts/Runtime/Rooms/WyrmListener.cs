using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[RequireComponent(typeof(AudioListener))]
public class WyrmListener : AmbiComponent<WyrmListener>
{
    protected override int MaximumCapacity => 1;

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

    protected override void LoadManagedToNative() { }

    protected override void RemoveNativeAtSwapBack(int removedIndex, int lastIndex) { }
}
