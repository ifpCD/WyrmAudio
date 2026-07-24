using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif
#pragma warning disable IDE1006 // Naming Styles

public interface IEasyCollider
{
    Transform transform { get; }

    BoxCollider BoxCollider { get; }

    Transform BottomLeft { get; }
    Transform TopRight { get; }

    EasyColliderState State { get; }

    Color OutlineColor { get; }
    Color VolumeColor { get; set; }

    Color OutlineSelected { get; }
    Color VolumeSelected { get; }
}

[System.Serializable]
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

    public float NearDistance = 5f;
    public float FarDistance = 20f;
}

public static class EasyColliderExtensions
{
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

#if UNITY_EDITOR
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

    public static float GetGizmoOpacity(this IEasyCollider self)
    {
        if (SceneView.currentDrawingSceneView == null)
            return 1f;

        Camera cam = SceneView.currentDrawingSceneView.camera;

        float distance = Vector3.Distance(cam.transform.position, self.transform.position);

        return Mathf.Lerp(1f, 0.1f, Mathf.InverseLerp(self.State.NearDistance, self.State.FarDistance, distance));
    }

    public static void DrawVolumeGizmo(this IEasyCollider self, bool selected)
    {
        if (self.BottomLeft == null || self.TopRight == null || self.BoxCollider == null)
            return;

        Gizmos.matrix = self.transform.localToWorldMatrix;

        Vector3 center = self.BoxCollider.center;
        Vector3 size = self.BoxCollider.size;

        float alpha = self.GetGizmoOpacity();

        Color outline = selected ? self.OutlineSelected : self.OutlineColor;
        Color volume = selected ? self.VolumeSelected : self.VolumeColor;

        outline.a *= alpha;
        volume.a *= alpha;

        Gizmos.color = outline;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = volume;
        Gizmos.DrawCube(center, size);
    }
#endif
}