using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

// User written
[AmbiSynchronizable]
[RequireComponent(typeof(BoxCollider))]
public partial class GraphNode : MonoBehaviour
{
    // BATCH SECTION
    [AmbiSync(nameof(ColliderExtents), AmbiSyncType.Input)]
    internal static NativeArray<float3> NodeExtents;

    internal static NativeReference<int> ListenerRoomID;

    [AmbiSync(nameof(IsInRoom), AmbiSyncType.Output)]
    internal static NativeArray<bool> IsInRooms;

    // jobs, unsafe pointer calls into C++, and completion here
    [AmbiProcessorHook]
    internal static void BatchUpdate() { }

    // MANAGED SECTION
    public BoxCollider BoxCollider { get; private set; }

    void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 ColliderExtents => BoxCollider.size * 0.5f;

    public bool IsInRoom = false;

    [AmbiManagedHook(AmbiManagedHookType.Awake)]
    internal void AmbiAwake() { }

    [AmbiManagedHook(AmbiManagedHookType.OnEnable)]
    internal void AmbiOnEnable() { }

    // work before batched update
    [AmbiManagedHook(AmbiManagedHookType.PreBatchUpdate)]
    internal void PreBatchUpdate() { }

    // work after batched update
    [AmbiManagedHook(AmbiManagedHookType.PostBatchUpdate)]
    internal void PostBatchUpdate() { }

    [AmbiManagedHook(AmbiManagedHookType.OnDisable)]
    internal void AmbiOnDisable() { }

    [AmbiManagedHook(AmbiManagedHookType.OnDestroy)]
    internal void AmbiOnDestroy() { }
}
