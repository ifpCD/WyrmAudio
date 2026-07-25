using System;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed partial class WyrmPortal : AmbiComponent<WyrmPortal>, IEasyCollider
{
    protected override int MaximumCapacity => 2000;

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

    public float _openness = 1f;

    [Range(0, 1)]
    public float Openness
    {
        get => _openness;
        set
        {
            _openness = value;
            PortalOpenness.SetIfEnabled(value, NativeIndex);
        }
    }

#if UNITY_EDITOR
    public Color OutlineColor => GizmoColors.FaintCyan;
    public Color OutlineSelected => GizmoColors.Cyan;

    public Color VolumeSelected => new(0, .1f, 1f, .05f);

    public Color VolumeColor { get; set; } = GizmoColors.None;

    GUIStyle labelStyle;

    void OnDrawGizmos()
    {
        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1, 1, 1, this.GetGizmoOpacity() * .2f);

        Handles.Label(transform.position, gameObject.name, labelStyle);

        this.DrawVolumeGizmo(false);
        DrawListenerRoomHalf();
    }

    void OnDrawGizmosSelected()
    {
        this.DrawVolumeGizmo(true);
        DrawListenerRoomHalf();
    }

    void DrawListenerRoomHalf()
    {
        if (WyrmListener.CompletelyInactive)
            return;

        int listenerRoomIdentifier = WyrmListener.ListenerRoomIdentifier.Value;
        float localZDirection;

        if (RoomA != null && listenerRoomIdentifier == RoomA.RoomIdentifier)
            localZDirection = -1f;
        else if (RoomB != null && listenerRoomIdentifier == RoomB.RoomIdentifier)
            localZDirection = 1f;
        else
            return;

        Vector3 halfSize = BoxCollider.size;
        halfSize.z *= .5f;

        Vector3 halfCenter = BoxCollider.center;
        halfCenter.z += localZDirection * halfSize.z * .5f;

        Color color = new(0, .5f, .5f, .5f);
        color.a *= this.GetGizmoOpacity();

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = color;
        Gizmos.DrawCube(halfCenter, halfSize);
    }

    void OnValidate()
    {
        if (!BoxCollider)
            BoxCollider = GetComponent<BoxCollider>();

        BoxCollider.enabled = false;
        this.ValidateCollider();
    }
#endif
}
