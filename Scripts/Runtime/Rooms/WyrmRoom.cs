using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public partial class WyrmRoomShape : EasyCollider
{
    GraphManager _owner;
    private int _nativeIndex = -1;

    [field: SerializeField]
    public int RoomIdentifier { get; internal set; }

    [Range(8, 32)]
    public int sampleCount = 32;

    [HideInInspector]
    public List<Vector3> samplesContainer;

    void OnDestroy() => DeregisterSelf();

    internal void InformOfRegistration(GraphManager owner, int myIndex)
    {
        _owner = owner;
        _nativeIndex = myIndex;
    }

    public void Populate()
    {
        _owner.ShapeWorldToLocal[_nativeIndex] = math.inverse(transform.localToWorldMatrix);
        _owner.ShapeExtents[_nativeIndex] = Extents;

        _owner.ShapeRoomIdentifier[_nativeIndex] = RoomIdentifier;
    }

    void DeregisterSelf() { }

#if UNITY_EDITOR
    protected override Color OutlineColor { get; set; } = GizmoColors.FaintYellow;
    protected override Color VolumeColor { get; set; } = new(0, 0, 0, 0f);

    protected override Color OutlineSelected { get; set; } = GizmoColors.Yellow;
    protected override Color VolumeSelected { get; set; } = new(0, 0.1f, 1, 0.05f);

    GUIStyle labelStyle;

    protected override void OnValidate()
    {
        base.OnValidate();

        samplesContainer ??= new List<Vector3>();

        Sampling.GenerateBoxVolumeSamples(BoxCollider, sampleCount, samplesContainer);
    }

    protected override void OnDrawGizmosSelected()
    {
        if (samplesContainer == null || samplesContainer.Count == 0)
            return;

        Gizmos.color = Color.green;

        foreach (Vector3 point in samplesContainer)
        {
            Gizmos.DrawSphere(point, 0.04f);
        }
        base.OnDrawGizmosSelected();
    }

    protected override void OnDrawGizmos()
    {
        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1f, 1f, 1f, GizmoOpacity);

        Handles.Label(transform.position, gameObject.name, labelStyle);
        base.OnDrawGizmos();
    }

    void Update()
    {
        var graphProvider = GraphManager.Instance;
        if (graphProvider == null)
        {
            VolumeColor = new(0, 0, 0, 0);
            return;
        }

        if (graphProvider.ListenerRoomIdentifier.Value == RoomIdentifier)
        {
            VolumeColor = new(0, 0.5f, 0.5f, 0.5f);
            return;
        }
    }
#endif
}
