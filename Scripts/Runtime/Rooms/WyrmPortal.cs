using System;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

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

    public float _portalClosenessRadius = 5f;

    [Range(1f, 5f)]
    public float PortalClosenessRadius
    {
        get => _portalClosenessRadius;
        set
        {
            if (_portalClosenessRadius == value)
                return;

            _portalClosenessRadius = value;

            if (IsRegistered)
                PortalClosenessRadiuses[SoAIndex] = value;
        }
    }
}
