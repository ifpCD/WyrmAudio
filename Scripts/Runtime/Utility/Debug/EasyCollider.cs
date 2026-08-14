using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable IDE1006 // Naming Styles

public interface IEasyCollider
{
    Transform transform { get; }

    BoxCollider BoxCollider { get; }

    Transform BottomLeft { get; }
    Transform TopRight { get; }

    EasyColliderState State { get; }

#if UNITY_EDITOR
    Color Volume { get; }
    Color Outline { get; }
#endif
}

[Serializable]
public class EasyColliderState
{
    public float VolumePadding = 0.01f;

    [HideInInspector]
    public Vector3 LastBottomLeftPos;

    [HideInInspector]
    public Vector3 LastTopRightPos;

    [HideInInspector]
    public Vector3 LastScale;

    [HideInInspector]
    public Quaternion LastRotation;

#if UNITY_EDITOR
    [NonSerialized]
    private GUIStyle _textStyle;

    public GUIStyle TextStyle
    {
        get
        {
            _textStyle ??= new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.white },
            };

            return _textStyle;
        }
    }
#endif
}

public static class EasyColliderExtensions
{
#if UNITY_EDITOR
    public static void UpdateCollider(this IEasyCollider self)
    {
        if (!self.BottomLeft || !self.TopRight || self.BoxCollider == null)
            return;

        Vector3 a = self.transform.InverseTransformPoint(self.BottomLeft.position);
        Vector3 b = self.transform.InverseTransformPoint(self.TopRight.position);

        Vector3 min = Vector3.Min(a, b) - Vector3.one * self.State.VolumePadding;
        Vector3 max = Vector3.Max(a, b) + Vector3.one * self.State.VolumePadding;

        self.BoxCollider.center = (min + max) * 0.5f;
        self.BoxCollider.size = max - min;
    }

    public static void RecenterPivotToCorners(this IEasyCollider self)
    {
        if (!self.BottomLeft || !self.TopRight)
            return;

        Vector3 center = (self.BottomLeft.position + self.TopRight.position) * 0.5f;

        Vector3 bl = self.BottomLeft.position;
        Vector3 tr = self.TopRight.position;

        self.transform.position = center;

        self.BottomLeft.position = bl;
        self.TopRight.position = tr;

        self.UpdateCollider();
    }

    public static bool ValidateCollider(this IEasyCollider self)
    {
        if (!self.BottomLeft || !self.TopRight)
            return false;

        bool changed =
            self.BottomLeft.position != self.State.LastBottomLeftPos
            || self.TopRight.position != self.State.LastTopRightPos
            || self.transform.localScale != self.State.LastScale
            || self.transform.rotation != self.State.LastRotation;

        if (!changed)
            return false;

        self.State.LastBottomLeftPos = self.BottomLeft.position;
        self.State.LastTopRightPos = self.TopRight.position;
        self.State.LastScale = self.transform.localScale;
        self.State.LastRotation = self.transform.rotation;

        self.UpdateCollider();
        return true;
    }

    public static float GetGizmoOpacity(this IEasyCollider self) => GetGizmoOpacity(self.transform.position);

    public static float GetGizmoOpacity(Vector3 gizmoPos)
    {
        var cam = Camera.current;

        if (cam == null)
            return 1f;

        var settings = WyrmAudioSettings.Instance;
        if (settings == null)
            return 1f;

        float distance = Vector3.Distance(cam.transform.position, gizmoPos);

        if (distance > settings.CutoffDistance)
            return 0f;
            
        float t = Mathf.InverseLerp(settings.NearDistance, settings.FarDistance, distance);

        return Mathf.Lerp(1f, 0.1f, t);
    }

    public static void DrawVolumeGizmo(this IEasyCollider self, bool selected, string label = default)
    {
        if (self.BottomLeft == null || self.TopRight == null || self.BoxCollider == null)
            return;

        Gizmos.matrix = self.transform.localToWorldMatrix;

        Vector3 center = self.BoxCollider.center;
        Vector3 size = self.BoxCollider.size;

        float gizmoAlpha = self.GetGizmoOpacity();

        Color outline = selected ? self.Outline.WithAlpha(0.8f) : self.Outline.WithAlpha(0.2f);
        Color volume = selected ? self.Volume.WithAlpha(0.8f) : self.Volume.WithAlpha(0.2f);

        outline.a *= gizmoAlpha;
        volume.a *= gizmoAlpha;

        if (outline.a != 0f)
        {
            Gizmos.color = outline;
            Gizmos.DrawWireCube(center, size);
        }

        // if (volume.a != 0f)
        // {
        //     Gizmos.color = volume;
        //     Gizmos.DrawCube(center, size);
        // }

        if (label != default)
        {
            var pos = self.transform.localToWorldMatrix.MultiplyPoint3x4(center);
            self.State.TextStyle.normal.textColor = self.State.TextStyle.normal.textColor.WithAlpha(gizmoAlpha);
            Handles.Label(pos, label, self.State.TextStyle);
        }
    }
#endif
}
