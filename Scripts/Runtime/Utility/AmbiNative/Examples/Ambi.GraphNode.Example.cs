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

    // track the array if this is needed in object oriented context after batch updates
    [AmbiSync(nameof(IsInRoom), AmbiSyncType.Output)]
    internal static NativeArray<bool> IsInRooms;

    // parallel jobs, unsafe pointer calls into C++, etc here
    [AmbiBatchHook]
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

    [AmbiHook(AmbiManagedHookType.Awake)]
    internal void AmbiAwake() { }

    [AmbiHook(AmbiManagedHookType.OnEnable)]
    internal void AmbiOnEnable() { }

    // work before batched update
    [AmbiHook(AmbiManagedHookType.PreBatchUpdate)]
    internal void PreBatchUpdate() { }

    // work after batched update
    [AmbiHook(AmbiManagedHookType.PostBatchUpdate)]
    internal void PostBatchUpdate() { }

    [AmbiHook(AmbiManagedHookType.OnDisable)]
    internal void AmbiOnDisable() { }

    [AmbiHook(AmbiManagedHookType.OnDestroy)]
    internal void AmbiOnDestroy() { }
}
