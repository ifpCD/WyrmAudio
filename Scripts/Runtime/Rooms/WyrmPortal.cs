using System;
using Unity.Mathematics;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[NoAutoStaticsCleanup]
public sealed partial class WyrmPortal : AmbiMonoBehaviour<WyrmPortal>, IEasyCollider
{
    protected override int AllocatedCapacity => 2000;

    [field: SerializeField]
    public BoxCollider BoxCollider { get; private set; }

    [field: SerializeField]
    public Transform BottomLeft { get; private set; }

    [field: SerializeField]
    public Transform TopRight { get; private set; }

    [HideInInspector]
    public EasyColliderState State { get; private set; } = new();

    [Header("Relation")]
    [field: SerializeField]
    public WyrmRoomShape RoomA { get; set; }

    [field: SerializeField]
    public WyrmRoomShape RoomB { get; set; }

    public float4x4 WorldToLocal => math.inverse(transform.localToWorldMatrix);
    public Vector3 Extents => BoxCollider.size * .5f;

    void OnEnable() => Register();

    void OnDisable() => Deregister();

    // In case transform/shape changes
    public void NotifyChange() => SoASync();

    public float _openness = 1f;

    [Range(0, 1)]
    public float Openness
    {
        get => _openness;
        set
        {
            if (_openness == value)
                return;

            _openness = value;

            if (IsRegistered)
                PortalOpenness[SoAIndex] = value;
        }
    }
}
