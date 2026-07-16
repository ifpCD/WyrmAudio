using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif

// Helper class to quickly set up rooms in the editor
[RequireComponent(typeof(BoxCollider))]
public abstract class EasyCollider : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Volume Generation")]
    [field: SerializeField] public Transform BottomLeft { get; private set; }
    [field: SerializeField] public Transform TopRight { get; private set; }
    [SerializeField] private float volumePadding = 0.01f;

    private Vector3 lastBottomLeftPos;
    private Vector3 lastTopRightPos;
    private Vector3 lastScale;
    private Quaternion lastRot;

    public BoxCollider BoxCollider { get; private set; }

    protected abstract Color OutlineColor { get; set; }
    protected abstract Color VolumeColor { get; set; }

    protected abstract Color OutlineSelected { get; set; }
    protected abstract Color VolumeSelected { get; set; }

    private void OnEnable()
    {
        BoxCollider.enabled = false;
        UpdateCollider();
    }

    private void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();

        if (!BottomLeft || !TopRight)
            return;

        bool changed =
            BottomLeft.position != lastBottomLeftPos ||
            TopRight.position != lastTopRightPos ||
            transform.localScale != lastScale ||
            transform.rotation != lastRot;

        if (changed)
        {
            lastBottomLeftPos = BottomLeft.position;
            lastTopRightPos = TopRight.position;
            lastScale = transform.localScale;
            lastRot = transform.rotation;

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


    private void OnDrawGizmos()
    {
        DrawVolumeGizmo(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawVolumeGizmo(true);
    }

    private void DrawVolumeGizmo(bool selected)
    {
        if (!BottomLeft || !TopRight)
            return;

        var t = transform;
        Gizmos.matrix = t.localToWorldMatrix;

        Vector3 center = BoxCollider.center;
        Vector3 size = BoxCollider.size;

        float alpha = 1f;

        if (SceneView.currentDrawingSceneView != null)
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            float distance = Vector3.Distance(cam.transform.position, t.position);

            // Adjust these values to control fade range
            float nearDistance = 5f;
            float farDistance = 20f;

            alpha = Mathf.Lerp(1f, 0.1f,
                Mathf.InverseLerp(nearDistance, farDistance, distance));
        }

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