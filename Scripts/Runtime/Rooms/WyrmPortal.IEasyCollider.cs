#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed partial class WyrmPortal : AmbiMonoBehaviour<WyrmPortal>, IEasyCollider
{
    public Color Outline { get; set; } = Color.cyan;
    public Color Volume => Color.clear;
    GUIStyle labelStyle;

    Color OpenColor = Color.green;
    Color ClosedColor = Color.red;

    string header;

#if WYRMAUDIO_VISUALIZATION_ENABLED
    void OnDrawGizmosSelected() => DrawPortal(true);

    void OnDrawGizmos() => DrawPortal(false);
#endif

    void DrawPortal(bool selected)
    {
        Outline = Color.Lerp(ClosedColor, OpenColor, Openness);

        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1, 1, 1, this.GetGizmoOpacity() * .2f);

        if (RoomA != null && RoomB != null)
        {
            header = $"{RoomA.gameObject.name} <-> {RoomB.gameObject.name}\n{Openness:F2}";
        }
        else
        {
            header = gameObject.name;
        }

        DrawBidirectionalForwardArrow(selected);
        this.DrawVolumeGizmo(false, header);
    }

    void DrawBidirectionalForwardArrow(bool selected)
    {
        const float arrowHeadSize = 0.5f;

        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        Color outline = selected ? Outline.WithAlpha(0.8f) : Outline.WithAlpha(0.2f);

        outline.a *= this.GetGizmoOpacity();

        Handles.color = outline;

        Handles.ArrowHandleCap(0, position, rotation, arrowHeadSize, EventType.Repaint);

        Handles.ArrowHandleCap(0, position, rotation * Quaternion.Euler(0f, 180f, 0f), arrowHeadSize, EventType.Repaint);
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
