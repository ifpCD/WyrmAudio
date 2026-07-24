using System;
using NUnit.Framework.Constraints;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public partial class WyrmPortal : EasyCollider
{
    GraphManager _owner;
    private int _nativeIndex = -1;

    [Header("Relation")]
    [field: SerializeField]
    public WyrmRoomShape RoomA { get; set; }

    [field: SerializeField]
    public WyrmRoomShape RoomB { get; set; }

    public Vector3 Extents => BoxCollider.size * 0.5f;

    internal void InformOfRegistration(GraphManager owner, int myIndex)
    {
        _owner = owner;
        _nativeIndex = myIndex;
    }

    internal void InformIndexChange(int myNewIndex) => _nativeIndex = myNewIndex;

    internal void Populate()
    {
        _owner.PortalWorldToLocal[_nativeIndex] = math.inverse(transform.localToWorldMatrix);
        _owner.PortalExtents[_nativeIndex] = Extents;

        _owner.PortalRoomA[_nativeIndex] = RoomA != null ? RoomA.RoomIdentifier : -1;
        _owner.PortalRoomB[_nativeIndex] = RoomB != null ? RoomB.RoomIdentifier : -1;

        _owner.PortalOpenness[_nativeIndex] = _openness;
    }

    void OnDestroy() => _owner.NotifyOfDestruction(this);

    public float _openness = 1f;

    [Header("State")]
    [Range(0f, 1f)]
    public float Openness
    {
        get => _openness;
        set
        {
            _openness = value;

            if (_nativeIndex == -1)
                return;

            _owner.PortalOpenness[_nativeIndex] = value;
        }
    }

#if UNITY_EDITOR
    protected override Color OutlineColor { get; set; } = GizmoColors.FaintCyan;
    protected override Color VolumeColor { get; set; } = GizmoColors.None;

    protected override Color OutlineSelected { get; set; } = GizmoColors.Cyan;
    protected override Color VolumeSelected { get; set; } = new(0, 0.1f, 1, 0.05f);

    GUIStyle labelStyle;

    protected override void OnDrawGizmos()
    {
        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1f, 1f, 1f, GizmoOpacity * 0.2f);

        Handles.Label(transform.position, gameObject.name, labelStyle);
        base.OnDrawGizmos();
    }
#endif
}
