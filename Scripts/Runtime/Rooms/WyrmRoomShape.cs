using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public partial class WyrmRoomShape : AmbiComponent<WyrmRoomShape>, IEasyCollider
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

    public float4x4 WorldToLocal => math.inverse(transform.localToWorldMatrix);
    public Vector3 Extents => BoxCollider.size * .5f;

    void OnEnable() => Register();

    void OnDisable() => Deregister();

    [field: SerializeField]
    public int RoomIdentifier { get; internal set; }

    [Range(8, 32)]
    public int sampleCount = 32;

    [HideInInspector]
    public List<Vector3> samplesContainer;

#if UNITY_EDITOR
    public Color OutlineColor => GizmoColors.FaintYellow;
    public Color OutlineSelected => GizmoColors.Yellow;

    public Color VolumeSelected => new(0, .1f, 1f, .05f);

    public Color VolumeColor { get; set; } = Color.clear;

    GUIStyle labelStyle;

    void OnDrawGizmos()
    {
        labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
        labelStyle.normal.textColor = new Color(1, 1, 1, this.GetGizmoOpacity());

        Handles.Label(transform.position, gameObject.name, labelStyle);

        this.DrawVolumeGizmo(false);
    }

    void OnDrawGizmosSelected()
    {
        if (samplesContainer != null)
        {
            Gizmos.color = Color.green;

            foreach (var p in samplesContainer)
                Gizmos.DrawSphere(p, .04f);
        }

        this.DrawVolumeGizmo(true);
    }

    void OnValidate()
    {
        if (BoxCollider == null)
            BoxCollider = GetComponent<BoxCollider>();
        this.ValidateCollider();

        samplesContainer ??= new List<Vector3>();
        HaltonSequence.GenerateBoxVolumeSamples(BoxCollider, sampleCount, samplesContainer);
    }

    void Update()
    {
        if (WyrmListener.CompletelyInactive)
            return;

        var ListenerRoomIdentifier = WyrmListener.ListenerRoomIdentifier.Value;
        if (ListenerRoomIdentifier == -1)
            return;

        VolumeColor = ListenerRoomIdentifier == RoomIdentifier ? new Color(0, .5f, .5f, .5f) : Color.clear;
    }

#endif
}
