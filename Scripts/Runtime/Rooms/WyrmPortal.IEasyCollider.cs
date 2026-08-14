#if UNITY_EDITOR
using UnityEngine;

public sealed partial class WyrmPortal : AmbiMonoBehaviour<WyrmPortal>, IEasyCollider
{
    public Color Outline { get; set; } = Color.cyan;
    public Color Volume => Color.clear;
    GUIStyle labelStyle;

    Color OpenColor = Color.green;
    Color ClosedColor = Color.red;

#if WYRMAUDIO_VISUALIZATION_ENABLED
    void OnDrawGizmos()
    {
        Outline = Color.Lerp(ClosedColor, OpenColor, Openness);

        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1, 1, 1, this.GetGizmoOpacity() * .2f);

        string header;

        if (RoomA != null && RoomB != null)
        {
            header = $"{RoomA.gameObject.name} <-> {RoomB.gameObject.name}";
        }
        else
        {
            header = gameObject.name;
        }

        this.DrawVolumeGizmo(false, $"{header}\n{Openness:F2}");
        // DrawListenerRoomHalf();
    }
#endif

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

        float alpha = this.GetGizmoOpacity() * 0.2f;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan.WithAlpha(alpha);
        Gizmos.DrawCube(halfCenter, halfSize);
    }

    void OnValidate()
    {
        if (!BoxCollider)
            BoxCollider = GetComponent<BoxCollider>();

        BoxCollider.enabled = false;
        this.ValidateCollider();
    }
}
#endif
