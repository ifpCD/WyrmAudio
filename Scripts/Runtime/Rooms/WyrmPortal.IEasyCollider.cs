using System;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed partial class WyrmPortal : AmbiMonoBehaviour<WyrmPortal>, IEasyCollider
{
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

        if (RoomA != null && RoomB != null)
        {
            Handles.Label(transform.position, $"{RoomA.gameObject.name} <-> {RoomB.gameObject.name}", labelStyle);
        }
        else
        {
            Handles.Label(transform.position, gameObject.name, labelStyle);
        }
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
        color.a *= this.GetGizmoOpacity() * 0.2f;

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
