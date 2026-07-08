using UnityEngine;

public partial class WyrmRoom : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Volume Generation")]
    [field: SerializeField] public Transform BottomLeft { get; private set; }
    [field: SerializeField] public Transform TopRight { get; private set; }
    [SerializeField] private float volumePadding = 0.01f;

    private BoxCollider boxCollider;

    private Vector3 lastBottomLeftPos;
    private Vector3 lastTopRightPos;
    private Vector3 lastScale;
    private Quaternion lastRot;


    private void OnEnable()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        UpdateCollider();
    }

    private void Update()
    {
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
        if (!BottomLeft || !TopRight || boxCollider == null)
            return;

        Vector3 a = transform.InverseTransformPoint(BottomLeft.position);
        Vector3 b = transform.InverseTransformPoint(TopRight.position);

        Vector3 min = Vector3.Min(a, b) - Vector3.one * volumePadding;
        Vector3 max = Vector3.Max(a, b) + Vector3.one * volumePadding;

        boxCollider.center = (min + max) * 0.5f;
        boxCollider.size = max - min;
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
        if (!BottomLeft || !TopRight)
            return;

        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        var t = transform;
        Gizmos.matrix = t.localToWorldMatrix;

        Vector3 center;
        Vector3 size;

        if (boxCollider != null)
        {
            center = boxCollider.center;
            size = boxCollider.size;
        }
        else
        {
            Vector3 a = t.InverseTransformPoint(BottomLeft.position);
            Vector3 b = t.InverseTransformPoint(TopRight.position);

            Vector3 min = Vector3.Min(a, b) - Vector3.one * volumePadding;
            Vector3 max = Vector3.Max(a, b) + Vector3.one * volumePadding;

            center = (min + max) * 0.5f;
            size = max - min;
        }

        Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}