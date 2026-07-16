using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public partial class WyrmPortal : EasyCollider
{
    GraphManager _owner;
    private int _nativeIndex = -1;

    [Header("Relation")]
    [field: SerializeField]
    public WyrmRoomShape RoomA { get; set; }

    [field: SerializeField]
    public WyrmRoomShape RoomB { get; set; }

    internal void InformOfRegistration(GraphManager owner, int myIndex)
    {
        _owner = owner;
        _nativeIndex = myIndex;
        Populate();
    }

    public void Populate()
    {
        _owner.PortalWorldToLocal[_nativeIndex] = math.inverse(transform.localToWorldMatrix);
        _owner.PortalExtents[_nativeIndex] = BoxCollider.size * 0.5f;
        _owner.PortalRoomA[_nativeIndex] = RoomA != null ? RoomA.RoomIdentifier : -1;
        _owner.PortalRoomB[_nativeIndex] = RoomB != null ? RoomB.RoomIdentifier : -1;
        _owner.PortalOpenness[_nativeIndex] = _openness;
    }

    public float _openness = 1f;

    [Header("State")]
    [Range(0f, 1f)]
    public float Openness
    {
        get => _openness;
        set
        {
            if (_openness == value) return;

            _openness = value;

            if (_nativeIndex == -1) return;

            _owner.PortalOpenness[_nativeIndex] = value;
        }
    }

    protected override Color OutlineColor { get; set; } = Color.cyan;
    protected override Color VolumeColor { get; set; } = new(0, 0, 0, 0f);

    protected override Color OutlineSelected { get; set; } = new(0, 0.1f, 1, 1f);
    protected override Color VolumeSelected { get; set; } = new(0, 0.1f, 1, 0.05f);
}