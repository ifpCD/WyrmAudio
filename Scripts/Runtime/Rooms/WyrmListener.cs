using Unity.Collections;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[NoAutoStaticsCleanup]
[RequireComponent(typeof(AudioListener))]
public class WyrmListener : MonoBehaviour
{
    public static NativeReference<int> ListenerRoomIdentifier;
    public static NativeReference<float3> ListenerPosition;
    public static NativeReference<quaternion> ListenerRotation;

    void OnEnable()
    {
        AllocateNative();
        WyrmAudioManager.NotifyListenerModified(this);
    }

    void OnDisable() => DeallocateNative();

    // csharpier-ignore
    void AllocateNative()
    {
        ListenerRoomIdentifier = new(allocator: Allocator.Persistent);
        ListenerPosition       = new(allocator: Allocator.Persistent);
        ListenerRotation       = new(allocator: Allocator.Persistent);
    }

    void DeallocateNative()
    {
        ListenerRoomIdentifier.TryDispose();
        ListenerPosition.TryDispose();
        ListenerRotation.TryDispose();
    }

    internal void Synchronize()
    {
        Transform listenerTransform = transform;
        ListenerPosition.Value = listenerTransform.position;
        ListenerRotation.Value = listenerTransform.rotation;
    }
}
