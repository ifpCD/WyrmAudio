using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif

// Helper class to quickly set up rooms in the editor
[RequireComponent(typeof(BoxCollider))]
public abstract class EasyCollider : MonoBehaviour
{
    public BoxCollider BoxCollider { get; protected set; }

#if UNITY_EDITOR
    [Header("Volume Generation")]
    [field: SerializeField]
    public Transform BottomLeft { get; private set; }

    [field: SerializeField]
    public Transform TopRight { get; private set; }

    [SerializeField]
    private float volumePadding = 0.01f;

    Vector3 _lastBottomLeftPos;
    Vector3 _lastTopRightPos;
    Vector3 _lastScale;
    Quaternion _lastRot;

    protected float NearDistance = 5f;
    protected float FarDistance = 20f;

    protected float GizmoOpacity
    {
        get
        {
            if (SceneView.currentDrawingSceneView == null)
                return 1f;

            Camera cam = SceneView.currentDrawingSceneView.camera;

            float distance = Vector3.Distance(cam.transform.position, transform.position);

            return Mathf.Lerp(1f, 0.1f, Mathf.InverseLerp(NearDistance, FarDistance, distance));
        }
    }

    protected abstract Color OutlineColor { get; set; }
    protected abstract Color VolumeColor { get; set; }

    protected abstract Color OutlineSelected { get; set; }
    protected abstract Color VolumeSelected { get; set; }

    void OnEnable()
    {
        BoxCollider.enabled = false;
        UpdateCollider();
    }

    protected virtual void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();

        if (!BottomLeft || !TopRight)
            return;

        bool changed =
            BottomLeft.position != _lastBottomLeftPos
            || TopRight.position != _lastTopRightPos
            || transform.localScale != _lastScale
            || transform.rotation != _lastRot;

        if (changed)
        {
            _lastBottomLeftPos = BottomLeft.position;
            _lastTopRightPos = TopRight.position;
            _lastScale = transform.localScale;
            _lastRot = transform.rotation;

            UpdateCollider();
        }
    }

    public void UpdateCollider()
    {
        if (!BottomLeft || !TopRight || BoxCollider == null)
            return;

        Vector3 a = transform.InverseTransformPoint(BottomLeft.position);
        Vector3 b = transform.InverseTransformPoint(TopRight.position);

        Vector3 min = Vector3.Min(a, b) - Vector3.one * volumePadding;
        Vector3 max = Vector3.Max(a, b) + Vector3.one * volumePadding;

        BoxCollider.center = (min + max) * 0.5f;
        BoxCollider.size = max - min;
    }

    public void RecenterPivotToCorners()
    {
        if (!BottomLeft || !TopRight)
            return;

        Vector3 center = (BottomLeft.position + TopRight.position) * 0.5f;

        Vector3 blWorld = BottomLeft.position;
        Vector3 trWorld = TopRight.position;

        transform.position = center;

        BottomLeft.position = blWorld;
        TopRight.position = trWorld;

        UpdateCollider();
    }

    protected virtual void OnDrawGizmos() => DrawVolumeGizmo(false);

    protected virtual void OnDrawGizmosSelected() => DrawVolumeGizmo(true);

    protected void DrawVolumeGizmo(bool selected)
    {
        if (!BottomLeft || !TopRight)
            return;

        var t = transform;
        Gizmos.matrix = t.localToWorldMatrix;

        Vector3 center = BoxCollider.center;
        Vector3 size = BoxCollider.size;

        float alpha = GizmoOpacity;

        Color outlineColor = selected ? OutlineSelected : OutlineColor;
        Color volumeColor = selected ? VolumeSelected : VolumeColor;

        outlineColor.a *= alpha;
        volumeColor.a *= alpha;

        Gizmos.color = outlineColor;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = volumeColor;
        Gizmos.DrawCube(center, size);
    }
#endif
}
